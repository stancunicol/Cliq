using System.Diagnostics;
using System.Net;
using Cliq.Api;
using Cliq.Domain;

namespace Cliq.IntegrationTests;

[TestFixture]
[NonParallelizable]
public sealed class BrowserPipelineTests
{
    [Test]
    [Category("Browser")]
    public async Task ScanReplayGenerateAndExecuteGeneratedNUnitWorkflow()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("CLIQ_RUN_BROWSER_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Set CLIQ_RUN_BROWSER_TESTS=true to run the real Chrome integration pipeline.");
        }

        using var workspace = new TemporaryWorkspace();
        await using var site = await FixtureWebServer.StartAsync();
        await using var host = await IntegrationApiHost.StartAsync();
        using var open = await host.Client.PostJsonAsync(
            "/api/v1/workspaces/open",
            new OpenWorkspaceRequest(workspace.RootPath));
        open.EnsureSuccessStatusCode();

        var settings = await host.Client.GetRequiredJsonAsync<CliqSettings>("/api/v1/settings");
        settings = settings with
        {
            TestExecution = settings.TestExecution with
            {
                HighlightPauseMs = 0,
                CaptureScreenshotOnFailure = false,
            },
            Scanning = settings.Scanning with { CaptureScreenshot = false },
        };
        using var saveSettings = await host.Client.PutJsonAsync("/api/v1/settings", settings);
        saveSettings.EnsureSuccessStatusCode();

        using var scanResponse = await host.Client.PostJsonAsync(
            "/api/v1/pages/scan",
            new ScanPageRequest(site.BaseAddress.AbsoluteUri, "Recorder fixture", Headless: true));
        scanResponse.EnsureSuccessStatusCode();
        var scan = await scanResponse.ReadRequiredJsonAsync<ScanPageResponse>();
        var workflow = BuildWorkflow(scan.Page, site.BaseAddress);
        using var saveTest = await host.Client.PutJsonAsync($"/api/v1/tests/{workflow.Id}", workflow);
        saveTest.EnsureSuccessStatusCode();

        using var start = await host.Client.PostJsonAsync(
            $"/api/v1/tests/{workflow.Id}/runs",
            new StartRunRequest(BrowserKind.Chrome));
        Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        var queued = await start.ReadRequiredJsonAsync<TestRun>();
        var replay = await WaitForCompletionAsync(host.Client, queued.Id);

        using var generationResponse = await host.Client.PostJsonAsync(
            "/api/v1/generation",
            new GenerationRequest(TestIds: [workflow.Id]));
        generationResponse.EnsureSuccessStatusCode();
        var generation = await generationResponse.ReadRequiredJsonAsync<GenerationResult>();
        var projectPath = PrepareGeneratedProject(workspace.RootPath);
        var generatedRun = await RunProcessAsync(
            "dotnet",
            ["test", projectPath, "--configuration", "Release", "--nologo"],
            workspace.RootPath,
            TimeSpan.FromMinutes(2));

        using var close = await host.Client.PostAsync("/api/v1/workspaces/close", null);

        Assert.Multiple(() =>
        {
            Assert.That(scan.Reconciliation.IsFirstScan, Is.True);
            Assert.That(scan.Page.Elements, Has.Count.GreaterThanOrEqualTo(6));
            Assert.That(replay.Status, Is.EqualTo(RunStatus.Passed), replay.Error);
            Assert.That(replay.Steps, Has.Count.EqualTo(workflow.Steps.Count));
            Assert.That(replay.Steps.All(step => step.Status == RunStatus.Passed), Is.True);
            Assert.That(generation.Artifacts.Any(artifact => artifact.Kind == "pageObject"), Is.True);
            Assert.That(generation.Artifacts.Any(artifact => artifact.Kind == "test"), Is.True);
            Assert.That(generatedRun.ExitCode, Is.Zero, generatedRun.CombinedOutput);
            Assert.That(generatedRun.CombinedOutput, Does.Contain("Passed!"));
            Assert.That(close.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        });
    }

    private static TestCase BuildWorkflow(PageDefinition page, Uri baseAddress)
    {
        var increment = page.Elements.Single(element =>
            element.Locator.Strategy == LocatorStrategy.TestId
            && element.Locator.Value == "increment-counter");
        var heading = page.Elements.Single(element => element.Tag == "h1");
        var displayName = page.Elements.Single(element =>
            element.Tag == "input" && element.Name.Contains("display", StringComparison.Ordinal));
        var role = page.Elements.Single(element => element.Tag == "select");
        var terms = page.Elements.Single(element => element.Checkbox);
        var continueLink = page.Elements.Single(element => element.Tag == "a");

        return new TestCase(
            TestCase.CurrentSchemaVersion,
            "browser-pipeline",
            "Browser pipeline",
            "Real Puppeteer, Selenium, generation, and generated NUnit coverage.",
            ["integration", "browser"],
            [
                new TestStep("navigate", StepAction.Navigate, "Open fixture", true, Url: baseAddress.AbsoluteUri),
                new TestStep("increment", StepAction.Click, "Increment", true, page.Id, increment.Id),
                new TestStep(
                    "counter",
                    StepAction.AssertText,
                    "Check counter",
                    true,
                    page.Id,
                    heading.Id,
                    Value: StepValue.Literal("Recorder fixture")),
                new TestStep(
                    "name",
                    StepAction.Fill,
                    "Enter name",
                    true,
                    page.Id,
                    displayName.Id,
                    Value: StepValue.Literal("Cliq integration")),
                new TestStep(
                    "role",
                    StepAction.Select,
                    "Select role",
                    true,
                    page.Id,
                    role.Id,
                    Value: StepValue.Literal("tester")),
                new TestStep(
                    "terms",
                    StepAction.Click,
                    "Accept terms",
                    true,
                    page.Id,
                    terms.Id,
                    Value: StepValue.Literal("true")),
                new TestStep("continue", StepAction.Click, "Continue", true, page.Id, continueLink.Id),
                new TestStep(
                    "url",
                    StepAction.AssertUrl,
                    "Check completion URL",
                    true,
                    Value: StepValue.Literal("/complete\\.html$")),
            ],
            DateTimeOffset.UtcNow);
    }

    private static async Task<TestRun> WaitForCompletionAsync(HttpClient client, string runId)
    {
        for (var attempt = 0; attempt < 600; attempt++)
        {
            var run = await client.GetRequiredJsonAsync<TestRun>($"/api/v1/runs/{runId}");
            if (run.Status is RunStatus.Passed or RunStatus.Failed or RunStatus.Cancelled)
            {
                return run;
            }

            await Task.Delay(100);
        }

        throw new AssertionException($"Browser run '{runId}' did not complete within one minute.");
    }

    private static string PrepareGeneratedProject(string workspaceRoot)
    {
        var template = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GeneratedProject.csproj.template");
        var destination = Path.Combine(workspaceRoot, "GeneratedProject.csproj");
        File.Copy(template, destination);
        return destination;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new AssertionException($"Process '{fileName}' exceeded the {timeout} timeout.");
        }

        var output = await outputTask;
        var error = await errorTask;
        return new ProcessResult(process.ExitCode, $"{output}{Environment.NewLine}{error}");
    }

    private sealed record ProcessResult(int ExitCode, string CombinedOutput);
}
