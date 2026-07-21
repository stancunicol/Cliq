using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using PageObjectGenerator.Api;
using PageObjectGenerator.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Permissive for the PoC: the API is meant to be called from a not-yet-built
// client, whose origin isn't known yet. Tighten before this leaves a dev box.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseWebSockets();

var outDir = SolutionPaths.DefaultOutputDirectory;
var screenshotDir = SolutionPaths.DefaultScreenshotDirectory;

app.MapPost("/api/page-objects", (GeneratePageObjectRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.BadRequest(new { error = "A valid absolute 'url' is required." });
        }

        var browser = PageObjectWorkflow.ParseBrowserKind(request.Browser);

        // Always derived from the URL, never caller-supplied: a free-text class
        // name or namespace would get spliced straight into generated C# source
        // and into the output file path, which is exactly the kind of input two
        // different vulnerabilities (source injection, path traversal) live in.
        var className = PageObjectWorkflow.DeriveClassName(request.Url);
        const string @namespace = "Generated";

        try
        {
            var result = PageObjectWorkflow.Generate(request.Url, browser, request.Headless, className, @namespace);
            var (pageObjectPath, testPath, _) = PageObjectFileWriter.Write(outDir, result);

            return Results.Created($"/api/page-objects/{result.ClassName}", new GeneratePageObjectResponse(
                result.ClassName, result.ElementCount, result.PageObjectCode, result.TestCode, pageObjectPath, testPath,
                ResponseMapping.ToScreenshotUrl(result.ScreenshotPath)));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Generation failed");
        }
    })
    .WithName("GeneratePageObject")
    .WithSummary("Launch a browser, scan a URL, and generate a Page Object + NUnit test.");

app.MapGet("/api/page-objects", () =>
    {
        var pages = PageObjectFileWriter.ListGenerated(outDir).Select(name => new GeneratedPageSummary(name));
        return Results.Ok(pages);
    })
    .WithName("ListPageObjects")
    .WithSummary("List previously generated Page Object classes.");

app.MapGet("/api/page-objects/{className}", (string className) =>
    {
        var (pageObjectCode, testCode) = PageObjectFileWriter.TryRead(outDir, className);
        if (pageObjectCode is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new GeneratedPageDetail(className, pageObjectCode, testCode));
    })
    .WithName("GetPageObject")
    .WithSummary("Fetch the source of a previously generated Page Object and its test.");

app.MapDelete("/api/page-objects/{className}", (string className) =>
    {
        var deleted = PageObjectFileWriter.Delete(outDir, className);
        return deleted ? Results.NoContent() : Results.NotFound();
    })
    .WithName("DeletePageObject")
    .WithSummary("Delete a previously generated Page Object, its test, and its metadata.");

app.MapGet("/api/page-objects/{className}/elements", (string className) =>
    {
        var metadata = PageObjectFileWriter.TryReadMetadata(outDir, className);
        if (metadata is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(metadata.Elements.Select(ResponseMapping.ToApi));
    })
    .WithName("GetPageObjectElements")
    .WithSummary("List the discovered elements (kind, locator, selector) for a previously generated Page Object, without the generated source.");

app.MapGet("/api/screenshots/{fileName}", (string fileName) =>
    {
        if (!IdentifierNaming.IsSafeFileName(fileName))
        {
            return Results.NotFound();
        }

        var path = Path.Combine(screenshotDir, fileName);
        return File.Exists(path) ? Results.File(path, "image/png") : Results.NotFound();
    })
    .WithName("GetScreenshot")
    .WithSummary("Fetch a screenshot captured during generation or a /run call.");

app.MapPost("/api/page-objects/{className}/run", (string className, RunPageObjectRequest? request) =>
    {
        var metadata = PageObjectFileWriter.TryReadMetadata(outDir, className);
        if (metadata is null)
        {
            return Results.NotFound(new { error = $"No generated page object named '{className}'." });
        }

        try
        {
            var headless = request?.Headless ?? false;
            var result = PageObjectRunner.Run(metadata, headless, request?.Values);
            return Results.Ok(ResponseMapping.ToApi(result));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Run failed");
        }
    })
    .WithName("RunPageObject")
    .WithSummary("Reopen the page and visually perform each discovered action (fill/click/select/check) in order.");

// WebSocket variant of /run: same execution, but each step is pushed to the
// client as it completes instead of only returning the final aggregate after
// 20-30+ seconds of silence. PageObjectRunner.Run is synchronous (it's driving
// a real browser), so it runs on a background thread and forwards steps
// through a Channel to the async loop actually allowed to await socket sends.
app.Map("/api/page-objects/{className}/run/ws", async (HttpContext context) =>
{
    var className = context.Request.RouteValues["className"] as string ?? "";

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var metadata = PageObjectFileWriter.TryReadMetadata(outDir, className);
    using var socket = await context.WebSockets.AcceptWebSocketAsync();

    if (metadata is null)
    {
        await SendJsonAsync(socket, new { type = "error", message = $"No generated page object named '{className}'." });
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "not found", CancellationToken.None);
        return;
    }

    // The client is expected to send one JSON text message right after connecting,
    // shaped like the POST /run body ({ "headless": ..., "values": {...} }).
    var request = await ReceiveJsonAsync<RunPageObjectRequest>(socket) ?? new RunPageObjectRequest();
    var channel = Channel.CreateUnbounded<StepResult>();

    var runTask = Task.Run(() => PageObjectRunner.Run(
        metadata, request.Headless, request.Values, screenshotDir: null, onStep: step => channel.Writer.TryWrite(step)));
    _ = runTask.ContinueWith(_ => channel.Writer.TryComplete(), TaskScheduler.Default);

    await foreach (var step in channel.Reader.ReadAllAsync())
    {
        await SendJsonAsync(socket, new { type = "step", step = ResponseMapping.ToApi(step) });
    }

    try
    {
        var result = await runTask;
        await SendJsonAsync(socket, new { type = "done", result = ResponseMapping.ToApi(result) });
    }
    catch (Exception ex)
    {
        await SendJsonAsync(socket, new { type = "error", message = ex.Message });
    }

    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
})
.WithName("RunPageObjectWithProgress");

app.Run();

static async Task SendJsonAsync(WebSocket socket, object payload)
{
    if (socket.State != WebSocketState.Open)
    {
        return;
    }

    var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
    await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
}

static async Task<T?> ReceiveJsonAsync<T>(WebSocket socket) where T : class
{
    try
    {
        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close || result.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(buffer.AsSpan(0, result.Count));
    }
    catch
    {
        return null;
    }
}
