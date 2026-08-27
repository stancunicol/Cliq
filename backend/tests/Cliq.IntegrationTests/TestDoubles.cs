using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Cliq.Application;
using Cliq.Domain;

namespace Cliq.IntegrationTests;

internal sealed class RecordingPageDiscovery(PageScanResult result) : IPageDiscovery
{
    internal ScanRequest? LastRequest { get; private set; }

    public Task<PageScanResult> ScanAsync(
        string url,
        string name,
        BrowserKind browser,
        bool headless,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = new ScanRequest(url, name, browser, headless);
        return Task.FromResult(result);
    }

    internal sealed record ScanRequest(string Url, string Name, BrowserKind Browser, bool Headless);
}

internal sealed class ControllableTestRunner : ITestRunner
{
    private readonly Channel<Invocation> _invocations = Channel.CreateUnbounded<Invocation>();

    internal ValueTask<Invocation> NextInvocationAsync(CancellationToken cancellationToken = default) =>
        _invocations.Reader.ReadAsync(cancellationToken);

    public async Task<TestRun> RunAsync(
        string runId,
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        BrowserKind browser,
        IReadOnlyDictionary<string, string> secrets,
        IProgress<StepResult> progress,
        CancellationToken cancellationToken)
    {
        var invocation = new Invocation(runId, test, pages, browser, secrets);
        await _invocations.Writer.WriteAsync(invocation, cancellationToken);
        await invocation.Release.Task.WaitAsync(cancellationToken);
        var step = new StepResult(test.Steps[0].Id, RunStatus.Passed, 3, LocateMs: 1, ActionMs: 2);
        progress.Report(step);
        return new TestRun(
            runId,
            test.Id,
            RunStatus.Passed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [step]);
    }

    public Task LocateElementAsync(
        PageDefinition page,
        string elementId,
        BrowserKind browser,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task LocateElementFromSourceAsync(
        string className,
        string locatorComment,
        BrowserKind browser,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string?> StreamHoveredElementAsync(
        PageDefinition page,
        BrowserKind browser,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield break;
    }

    internal sealed record Invocation(
        string RunId,
        TestCase Test,
        IReadOnlyDictionary<string, PageDefinition> Pages,
        BrowserKind Browser,
        IReadOnlyDictionary<string, string> Secrets)
    {
        internal TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
