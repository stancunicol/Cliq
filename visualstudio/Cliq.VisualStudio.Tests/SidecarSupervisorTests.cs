using Cliq.VisualStudio.Core.Runtime;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class SidecarSupervisorTests
{
    [Test]
    public async Task EnsureStarted_IsLazySingletonWithSecureEnvironmentAndHealthCheck()
    {
        var process = FakeProcess.Ready(101, "http://127.0.0.1:4567");
        var factory = new FakeProcessFactory(process);
        var health = new FakeHealthProbe();
        var supervisor = CreateSupervisor(factory, health);

        Assert.That(factory.Starts, Is.Empty);
        var first = await supervisor.EnsureStartedAsync(CancellationToken.None);
        var second = await supervisor.EnsureStartedAsync(CancellationToken.None);

        var start = factory.Starts.Single();
        var token = start.Environment["CLIQ_API_TOKEN"];
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.Address.AbsoluteUri, Is.EqualTo("http://127.0.0.1:4567/"));
            Assert.That(first.ProcessId, Is.EqualTo(101));
            Assert.That(token, Has.Length.EqualTo(43));
            Assert.That(token, Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(start.Environment["CLIQ_HOST_KIND"], Is.EqualTo("visualstudio"));
            Assert.That(start.Arguments, Does.Contain("127.0.0.1:0"));
            Assert.That(health.Addresses, Is.EqualTo(new[] { first.Address }));
            Assert.That(factory.Starts, Has.Count.EqualTo(1));
        });

        await supervisor.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task PackagedPaths_ArePassedAsScannerEnvironment()
    {
        var process = FakeProcess.Ready(102, "http://localhost:4568");
        var factory = new FakeProcessFactory(process);
        var nodePath = typeof(SidecarSupervisorTests).Assembly.Location;
        var options = DefaultOptions();
        options.NodeExecutablePath = nodePath;
        options.ScannerScriptPath = nodePath;
        var supervisor = new SidecarSupervisor(options, factory, new FakeHealthProbe());

        await supervisor.EnsureStartedAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                factory.Starts[0].Environment["CLIQ_NODE_PATH"],
                Is.EqualTo(Path.GetFullPath(nodePath)));
            Assert.That(
                factory.Starts[0].Environment["CLIQ_PUPPETEER_SCANNER_PATH"],
                Is.EqualTo(Path.GetFullPath(nodePath)));
        });
        await supervisor.StopAsync(CancellationToken.None);
    }

    [Test]
    public void StartupTimeout_KillsAndDisposesProcess()
    {
        var process = FakeProcess.WithBlockedOutput(103);
        var factory = new FakeProcessFactory(process);
        var options = DefaultOptions();
        options.StartupTimeout = TimeSpan.FromMilliseconds(40);
        var supervisor = new SidecarSupervisor(options, factory, new FakeHealthProbe());

        var exception = Assert.ThrowsAsync<SidecarStartupException>(
            async () => await supervisor.EnsureStartedAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<TimeoutException>());
            Assert.That(process.KillCount, Is.EqualTo(1));
            Assert.That(process.IsDisposed, Is.True);
        });
    }

    [Test]
    public void InvalidReadyAddress_IsRejectedAndProcessCleanedUp()
    {
        var process = FakeProcess.Ready(104, "https://example.com:4567");
        var supervisor = CreateSupervisor(
            new FakeProcessFactory(process),
            new FakeHealthProbe());

        Assert.ThrowsAsync<SidecarStartupException>(
            async () => await supervisor.EnsureStartedAsync(CancellationToken.None));
        Assert.Multiple(() =>
        {
            Assert.That(process.KillCount, Is.EqualTo(1));
            Assert.That(process.IsDisposed, Is.True);
        });
    }

    [Test]
    public void ExitBeforeReady_ReportsExitAndCleansUp()
    {
        var process = FakeProcess.WithBlockedOutput(105);
        process.Exit(23);
        var supervisor = CreateSupervisor(
            new FakeProcessFactory(process),
            new FakeHealthProbe());

        var exception = Assert.ThrowsAsync<SidecarStartupException>(
            async () => await supervisor.EnsureStartedAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("exit code 23"));
            Assert.That(process.IsDisposed, Is.True);
        });
    }

    [Test]
    public async Task Crash_ResetsStateAndNextCommandRestartsOnce()
    {
        var first = FakeProcess.Ready(106, "http://127.0.0.1:4569");
        var second = FakeProcess.Ready(107, "http://127.0.0.1:4570");
        var factory = new FakeProcessFactory(first, second);
        var diagnostics = new List<string>();
        var supervisor = CreateSupervisor(factory, new FakeHealthProbe(), diagnostics.Add);

        var initial = await supervisor.EnsureStartedAsync(CancellationToken.None);
        first.Exit(9);
        await WaitUntilAsync(() => first.IsDisposed);
        var restarted = await supervisor.EnsureStartedAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(initial.ProcessId, Is.EqualTo(106));
            Assert.That(restarted.ProcessId, Is.EqualTo(107));
            Assert.That(factory.Starts, Has.Count.EqualTo(2));
            Assert.That(diagnostics, Has.Some.Contains("restart on the next command"));
        });

        await supervisor.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Stop_KillsEntireTreeAndDisposes()
    {
        var process = FakeProcess.Ready(108, "http://127.0.0.1:4571");
        var supervisor = CreateSupervisor(
            new FakeProcessFactory(process),
            new FakeHealthProbe());
        await supervisor.EnsureStartedAsync(CancellationToken.None);

        await supervisor.StopAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(process.KillCount, Is.EqualTo(1));
            Assert.That(process.IsDisposed, Is.True);
        });
    }

    [Test]
    public void Sanitizer_RemovesBearerAndKnownToken()
    {
        const string token = "private-sidecar-token";
        var message = $"request Authorization: Bearer abc.def and CLIQ_API_TOKEN={token}";

        var sanitized = SidecarDiagnosticSanitizer.Sanitize(message, new[] { token });

        Assert.Multiple(() =>
        {
            Assert.That(sanitized, Does.Not.Contain("abc.def"));
            Assert.That(sanitized, Does.Not.Contain(token));
            Assert.That(sanitized, Does.Contain("[REDACTED]"));
        });
    }

    [Test]
    public async Task StartupOutput_IsSanitizedBeforeDiagnostics()
    {
        var process = FakeProcess.Ready(
            109,
            "http://127.0.0.1:4572",
            "Authorization: Bearer stdout-secret");
        var diagnostics = new List<string>();
        var supervisor = CreateSupervisor(
            new FakeProcessFactory(process),
            new FakeHealthProbe(),
            diagnostics.Add);

        await supervisor.EnsureStartedAsync(CancellationToken.None);

        Assert.That(string.Join("\n", diagnostics), Does.Not.Contain("stdout-secret"));
        await supervisor.StopAsync(CancellationToken.None);
    }

    [Test]
    public void HealthFailure_CleansUpAndSurfacesStartupFailure()
    {
        var process = FakeProcess.Ready(110, "http://127.0.0.1:4573");
        var health = new FakeHealthProbe
        {
            Error = new HttpRequestException("connection refused"),
        };
        var supervisor = CreateSupervisor(new FakeProcessFactory(process), health);

        var exception = Assert.ThrowsAsync<SidecarStartupException>(
            async () => await supervisor.EnsureStartedAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<HttpRequestException>());
            Assert.That(process.KillCount, Is.EqualTo(1));
            Assert.That(process.IsDisposed, Is.True);
        });
    }

    private static SidecarSupervisor CreateSupervisor(
        FakeProcessFactory factory,
        FakeHealthProbe health,
        Action<string>? diagnostic = null) =>
        new(DefaultOptions(), factory, health, diagnostic);

    private static SidecarOptions DefaultOptions() =>
        new()
        {
            PackagedExecutablePath = typeof(SidecarSupervisorTests).Assembly.Location,
            WorkingDirectory = TestContext.CurrentContext.WorkDirectory,
            StartupTimeout = TimeSpan.FromSeconds(1),
        };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class FakeHealthProbe : ISidecarHealthProbe
    {
        public List<Uri> Addresses { get; } = new();
        public Exception? Error { get; set; }

        public Task CheckReadyAsync(Uri address, CancellationToken cancellationToken)
        {
            Addresses.Add(address);
            return Error == null ? Task.CompletedTask : Task.FromException(Error);
        }
    }

    private sealed class FakeProcessFactory : ISidecarProcessFactory
    {
        private readonly Queue<FakeProcess> _processes;

        public FakeProcessFactory(params FakeProcess[] processes) =>
            _processes = new Queue<FakeProcess>(processes);

        public List<SidecarProcessStartInfo> Starts { get; } = new();

        public ISidecarProcess Start(SidecarProcessStartInfo startInfo)
        {
            Starts.Add(startInfo);
            return _processes.Dequeue();
        }
    }

    private sealed class FakeProcess : ISidecarProcess
    {
        private readonly Queue<string?> _output;
        private readonly TaskCompletionSource<string?> _blockedOutput =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<int> _exit =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private FakeProcess(int id, IEnumerable<string?> output)
        {
            Id = id;
            _output = new Queue<string?>(output);
        }

        public int Id { get; }
        public bool HasExited => _exit.Task.IsCompleted;
        public Task<int> ExitTask => _exit.Task;
        public int KillCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public static FakeProcess Ready(int id, string address, params string[] precedingOutput) =>
            new(
                id,
                precedingOutput
                    .Cast<string?>()
                    .Concat(new[] { $"CLIQ_READY {{\"address\":\"{address}\"}}" }));

        public static FakeProcess WithBlockedOutput(int id) => new(id, Array.Empty<string?>());

        public Task<string?> ReadOutputLineAsync() =>
            _output.Count > 0 ? Task.FromResult(_output.Dequeue()) : _blockedOutput.Task;

        public Task<string?> ReadErrorLineAsync() => Task.FromResult<string?>(null);

        public void KillTree()
        {
            KillCount++;
            Exit(-1);
        }

        public void Exit(int exitCode) => _exit.TrySetResult(exitCode);

        public void Dispose() => IsDisposed = true;
    }
}
