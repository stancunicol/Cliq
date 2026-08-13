using System.Net;
using System.Net.Http.Json;
using Cliq.Api;
using Cliq.Application;
using Cliq.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cliq.IntegrationTests;

[TestFixture]
public sealed class ApiIntegrationTests
{
    [Test]
    public async Task HealthAndAuthentication_EnforceTheSidecarBoundary()
    {
        await using var host = await IntegrationApiHost.StartAsync();

        using var health = await host.UnauthenticatedClient.GetAsync("/health/ready");
        using var unauthorized = await host.UnauthenticatedClient.GetAsync("/api/v1/workspace");
        using var authorized = await host.Client.GetAsync("/api/v1/workspace");
        var unauthorizedProblem = await unauthorized.ReadRequiredJsonAsync<ProblemDetails>();
        var authorizedProblem = await authorized.ReadRequiredJsonAsync<ProblemDetails>();

        Assert.Multiple(() =>
        {
            Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(unauthorizedProblem.Title, Is.EqualTo("Unauthorized"));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(authorizedProblem.Detail, Is.EqualTo("No Cliq workspace is open."));
        });
    }

    [Test]
    public async Task WorkspaceLifecycle_PersistsEntitiesGeneratesArtifactsAndReturnsContractErrors()
    {
        using var workspace = new TemporaryWorkspace();
        await using var host = await IntegrationApiHost.StartAsync();

        using var open = await host.Client.PostJsonAsync(
            "/api/v1/workspaces/open",
            new OpenWorkspaceRequest(workspace.RootPath));
        open.EnsureSuccessStatusCode();
        var snapshot = await open.ReadRequiredJsonAsync<WorkspaceSnapshot>();

        var project = snapshot.Project with
        {
            Name = "Integration workspace",
            BaseUrl = "https://example.test/",
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        using var saveProject = await host.Client.PutJsonAsync("/api/v1/project", project);
        saveProject.EnsureSuccessStatusCode();

        var settings = await host.Client.GetRequiredJsonAsync<CliqSettings>("/api/v1/settings");
        settings = settings with
        {
            TestExecution = settings.TestExecution with { HighlightPauseMs = 0 },
        };
        using var saveSettings = await host.Client.PutJsonAsync("/api/v1/settings", settings);
        saveSettings.EnsureSuccessStatusCode();

        var page = TestData.Page();
        var test = TestData.Test();
        using var savePage = await host.Client.PutJsonAsync($"/api/v1/pages/{page.Id}", page);
        savePage.EnsureSuccessStatusCode();
        using var saveTest = await host.Client.PutJsonAsync($"/api/v1/tests/{test.Id}", test);
        saveTest.EnsureSuccessStatusCode();
        using var saveSecrets = await host.Client.PutJsonAsync(
            "/api/v1/secrets",
            new Dictionary<string, string> { ["username"] = "integration-user" });
        saveSecrets.EnsureSuccessStatusCode();

        var persistedProject = await host.Client.GetRequiredJsonAsync<ProjectManifest>("/api/v1/project");
        var pages = await host.Client.GetRequiredJsonAsync<IReadOnlyList<PageDefinition>>("/api/v1/pages");
        var tests = await host.Client.GetRequiredJsonAsync<IReadOnlyList<TestCase>>("/api/v1/tests");
        var secrets = await host.Client.GetRequiredJsonAsync<Dictionary<string, string>>("/api/v1/secrets");

        using var generationResponse = await host.Client.PostJsonAsync(
            "/api/v1/generation",
            new GenerationRequest(TestIds: [test.Id]));
        generationResponse.EnsureSuccessStatusCode();
        var generation = await generationResponse.ReadRequiredJsonAsync<GenerationResult>();
        var pageObject = generation.Artifacts.Single(artifact => artifact.Kind == "pageObject");
        var generatedTest = generation.Artifacts.Single(artifact => artifact.Kind == "test");

        using var pageIdMismatch = await host.Client.PutJsonAsync("/api/v1/pages/wrong-id", page);
        using var testIdMismatch = await host.Client.PutJsonAsync("/api/v1/tests/wrong-id", test);
        using var runIdMismatch = await host.Client.PutJsonAsync(
            "/api/v1/runs/wrong-id",
            new TestRun("run-id", test.Id, RunStatus.Passed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, []));
        using var missingPage = await host.Client.GetAsync("/api/v1/pages/missing");
        using var invalidTest = await host.Client.PutJsonAsync(
            "/api/v1/tests/invalid",
            test with
            {
                Id = "invalid",
                Steps =
                [
                    new TestStep("missing", StepAction.Click, "Missing", true, page.Id, "missing"),
                ],
            });

        Assert.Multiple(() =>
        {
            Assert.That(persistedProject.Name, Is.EqualTo("Integration workspace"));
            Assert.That(pages.Select(item => item.Id), Is.EquivalentTo(new[] { page.Id }));
            Assert.That(tests.Select(item => item.Id), Is.EquivalentTo(new[] { test.Id }));
            Assert.That(secrets, Does.ContainKey("username").WithValue("integration-user"));
            Assert.That(File.ReadAllText(pageObject.Path), Does.Contain("public sealed class LoginPage"));
            Assert.That(File.ReadAllText(generatedTest.Path), Does.Contain("public sealed class LoginFlowTests"));
            Assert.That(pageIdMismatch.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testIdMismatch.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(runIdMismatch.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(missingPage.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(invalidTest.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });

        using var deleteSecret = await host.Client.DeleteAsync("/api/v1/secrets/username");
        using var deleteTest = await host.Client.DeleteAsync($"/api/v1/tests/{test.Id}");
        using var deletePage = await host.Client.DeleteAsync($"/api/v1/pages/{page.Id}");
        using var close = await host.Client.PostAsync("/api/v1/workspaces/close", null);
        using var afterClose = await host.Client.GetAsync("/api/v1/workspace");

        Assert.Multiple(() =>
        {
            Assert.That(deleteSecret.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(deleteTest.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(deletePage.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(close.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(afterClose.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        });
    }

    [Test]
    public async Task Scan_PassesResolvedDefaultsAndPersistsTheDiscoveryResult()
    {
        using var workspace = new TemporaryWorkspace();
        var page = TestData.Page("https://example.test/scanned");
        var suggestion = new DynamicElementSuggestion(
            "row_action",
            StepAction.Click,
            LocatorStrategy.XPath,
            "//tr[{param}]/td/button",
            4);
        var discovery = new RecordingPageDiscovery(
            new PageScanResult(
                page,
                new ScanReconciliationReport(0, page.Elements.Count, 0, IsFirstScan: true),
                DynamicElementSuggestions: [suggestion],
                ScreenshotPath: "scan.png"));
        await using var host = await IntegrationApiHost.StartAsync(services =>
        {
            services.RemoveAll<IPageDiscovery>();
            services.AddSingleton<IPageDiscovery>(discovery);
        });
        using var open = await host.Client.PostJsonAsync(
            "/api/v1/workspaces/open",
            new OpenWorkspaceRequest(workspace.RootPath));
        open.EnsureSuccessStatusCode();

        using var response = await host.Client.PostJsonAsync(
            "/api/v1/pages/scan",
            new ScanPageRequest(page.Url, page.Name, Headless: false));
        response.EnsureSuccessStatusCode();
        var scan = await response.ReadRequiredJsonAsync<ScanPageResponse>();
        var persisted = await host.Client.GetRequiredJsonAsync<PageDefinition>($"/api/v1/pages/{page.Id}");

        Assert.Multiple(() =>
        {
            Assert.That(discovery.LastRequest, Is.Not.Null);
            Assert.That(discovery.LastRequest!.Browser, Is.EqualTo(BrowserKind.Chrome));
            Assert.That(discovery.LastRequest.Headless, Is.False);
            Assert.That(scan.Page.Id, Is.EqualTo(page.Id));
            Assert.That(scan.Reconciliation.IsFirstScan, Is.True);
            Assert.That(scan.DynamicElementSuggestions, Has.Count.EqualTo(1));
            Assert.That(scan.ScreenshotPath, Is.EqualTo("scan.png"));
            Assert.That(persisted.Elements, Has.Count.EqualTo(page.Elements.Count));
        });
    }

    [Test]
    public async Task Runs_StreamEventsPersistCompletionAndSupportConflictAndCancellation()
    {
        using var workspace = new TemporaryWorkspace();
        var runner = new ControllableTestRunner();
        await using var host = await IntegrationApiHost.StartAsync(services =>
        {
            services.RemoveAll<ITestRunner>();
            services.AddSingleton<ITestRunner>(runner);
        });
        await OpenWorkspaceWithTestAsync(host.Client, workspace.RootPath);
        using var saveSecrets = await host.Client.PutJsonAsync(
            "/api/v1/secrets",
            new Dictionary<string, string> { ["token"] = "secret-value" });
        saveSecrets.EnsureSuccessStatusCode();

        using var start = await host.Client.PostJsonAsync(
            "/api/v1/tests/login-flow/runs",
            new StartRunRequest());
        Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        var queued = await start.ReadRequiredJsonAsync<TestRun>();
        var invocation = await runner.NextInvocationAsync();
        using var conflictingStart = await host.Client.PostJsonAsync(
            "/api/v1/tests/login-flow/runs",
            new StartRunRequest());
        var eventsTask = host.Client.GetStringAsync($"/api/v1/runs/{queued.Id}/events");
        invocation.Release.SetResult();
        var events = await eventsTask;
        var completed = await host.Client.GetRequiredJsonAsync<TestRun>($"/api/v1/runs/{queued.Id}");

        Assert.Multiple(() =>
        {
            Assert.That(start.Headers.Location?.ToString(), Is.EqualTo($"/api/v1/runs/{queued.Id}"));
            Assert.That(conflictingStart.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(invocation.Browser, Is.EqualTo(BrowserKind.Chrome));
            Assert.That(invocation.Secrets, Does.ContainKey("token").WithValue("secret-value"));
            Assert.That(events, Does.Contain("event: step"));
            Assert.That(events, Does.Contain("event: done"));
            Assert.That(completed.Status, Is.EqualTo(RunStatus.Passed));
            Assert.That(completed.Steps, Has.Count.EqualTo(1));
            Assert.That(
                File.Exists(Path.Combine(workspace.RootPath, ".cliq", "runs", queued.Id, "run.json")),
                Is.True);
        });

        using var cancelStart = await StartAfterRunGateReleasesAsync(host.Client);
        var cancellable = await cancelStart.ReadRequiredJsonAsync<TestRun>();
        _ = await runner.NextInvocationAsync();
        using var cancel = await host.Client.PostAsync($"/api/v1/runs/{cancellable.Id}/cancel", null);
        var cancelled = await WaitForStatusAsync(host.Client, cancellable.Id, RunStatus.Cancelled);
        using var missingCancel = await host.Client.PostAsync("/api/v1/runs/missing/cancel", null);
        await WaitForPersistedStatusAsync(workspace.RootPath, cancellable.Id, RunStatus.Cancelled);
        using var close = await host.Client.PostAsync("/api/v1/workspaces/close", null);

        Assert.Multiple(() =>
        {
            Assert.That(cancel.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(cancelled.Error, Is.EqualTo("Run cancelled."));
            Assert.That(missingCancel.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(close.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        });
    }

    private static async Task OpenWorkspaceWithTestAsync(HttpClient client, string rootPath)
    {
        using var open = await client.PostJsonAsync("/api/v1/workspaces/open", new OpenWorkspaceRequest(rootPath));
        open.EnsureSuccessStatusCode();
        var page = TestData.Page();
        var test = TestData.Test();
        using var savePage = await client.PutJsonAsync($"/api/v1/pages/{page.Id}", page);
        savePage.EnsureSuccessStatusCode();
        using var saveTest = await client.PutJsonAsync($"/api/v1/tests/{test.Id}", test);
        saveTest.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> StartAfterRunGateReleasesAsync(HttpClient client)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await client.PostJsonAsync(
                "/api/v1/tests/login-flow/runs",
                new StartRunRequest());
            if (response.StatusCode != HttpStatusCode.Conflict)
            {
                response.EnsureSuccessStatusCode();
                return response;
            }

            response.Dispose();
            await Task.Delay(25);
        }

        throw new AssertionException("The previous run did not release the run gate.");
    }

    private static async Task<TestRun> WaitForStatusAsync(
        HttpClient client,
        string runId,
        RunStatus expected)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var run = await client.GetRequiredJsonAsync<TestRun>($"/api/v1/runs/{runId}");
            if (run.Status == expected)
            {
                return run;
            }

            await Task.Delay(25);
        }

        throw new AssertionException($"Run '{runId}' did not reach {expected}.");
    }

    private static async Task WaitForPersistedStatusAsync(
        string workspaceRoot,
        string runId,
        RunStatus expected)
    {
        var path = Path.Combine(workspaceRoot, ".cliq", "runs", runId, "run.json");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    await using var stream = File.OpenRead(path);
                    var run = await System.Text.Json.JsonSerializer.DeserializeAsync<TestRun>(
                        stream,
                        IntegrationJson.Options);
                    if (run?.Status == expected)
                    {
                        return;
                    }
                }
            }
            catch (IOException)
            {
                // The repository may be between its temporary-file write and atomic replace.
            }

            await Task.Delay(25);
        }

        throw new AssertionException($"Run '{runId}' was not persisted as {expected}.");
    }
}
