using System.Collections.Concurrent;
using System.Threading.Channels;
using Cliq.Domain;

namespace Cliq.Application;

public sealed class RunCoordinator(
    IWorkspaceRepository repository,
    ITestRunner runner,
    ISecretStore secrets) : IDisposable
{
    // How long a finished run's state (events channel + cancellation token) stays resident after
    // completion, so a client that's mid-poll for its final status/events doesn't have it vanish
    // out from under it. Swept on the next StartAsync rather than on a timer - this is a
    // single-workspace sidecar process, so "another run just started" is the natural point to
    // reclaim the previous one.
    private static readonly TimeSpan CompletedRunRetention = TimeSpan.FromMinutes(30);

    private readonly SemaphoreSlim _runGate = new(1, 1);
    private readonly ConcurrentDictionary<string, RunState> _runs = new(StringComparer.Ordinal);

    public async Task<TestRun> StartAsync(string testId, BrowserKind? browser, CancellationToken cancellationToken)
    {
        if (!await _runGate.WaitAsync(0, cancellationToken))
        {
            throw new RunAlreadyActiveException();
        }

        try
        {
            EvictCompletedRuns();

            var snapshot = await repository.GetSnapshotAsync(cancellationToken);
            var test = snapshot.Tests.SingleOrDefault(item => item.Id == testId)
                ?? throw new KeyNotFoundException($"Test '{testId}' was not found.");
            var run = new TestRun(
                Guid.NewGuid().ToString("N"),
                test.Id,
                RunStatus.Queued,
                DateTimeOffset.UtcNow,
                null,
                []);
            // Only tracked once the row is durably saved - if SaveRunAsync throws, there is no
            // RunState left behind that would sit in _runs forever stuck at Queued with nothing
            // to ever move it to a terminal status.
            await repository.SaveRunAsync(run, cancellationToken);

            var state = new RunState(run);
            _runs[run.Id] = state;

            _ = ExecuteAsync(
                state,
                test,
                snapshot,
                browser ?? snapshot.Project.DefaultBrowser,
                CancellationToken.None);
            return run;
        }
        catch
        {
            _runGate.Release();
            throw;
        }
    }

    private void EvictCompletedRuns()
    {
        var cutoff = DateTimeOffset.UtcNow - CompletedRunRetention;
        foreach (var (id, state) in _runs)
        {
            // CompletedAt is only ever set once ExecuteAsync reaches a terminal status (Passed/
            // Failed/Cancelled) - a still-Queued or Running entry always has a null CompletedAt
            // and is never a candidate for eviction here.
            if (state.Run.CompletedAt is { } completedAt &&
                completedAt < cutoff &&
                _runs.TryRemove(id, out var removed))
            {
                removed.Cancellation.Dispose();
            }
        }
    }

    public TestRun? Get(string runId) =>
        _runs.TryGetValue(runId, out var state) ? state.Run : null;

    public IAsyncEnumerable<StepResult> ReadEventsAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_runs.TryGetValue(runId, out var state))
        {
            throw new KeyNotFoundException($"Run '{runId}' was not found.");
        }

        return state.Events.Reader.ReadAllAsync(cancellationToken);
    }

    public bool Cancel(string runId)
    {
        if (!_runs.TryGetValue(runId, out var state))
        {
            return false;
        }

        state.Cancellation.Cancel();
        return true;
    }

    public void Dispose()
    {
        foreach (var state in _runs.Values)
        {
            state.Cancellation.Cancel();
            state.Cancellation.Dispose();
        }

        _runGate.Dispose();
    }

    private async Task ExecuteAsync(
        RunState state,
        TestCase test,
        WorkspaceSnapshot snapshot,
        BrowserKind browser,
        CancellationToken cancellationToken)
    {
        try
        {
            state.Run = state.Run with { Status = RunStatus.Running };
            await repository.SaveRunAsync(state.Run, cancellationToken);
            var secretValues = await secrets.ReadAsync(cancellationToken);
            var pages = snapshot.Pages.ToDictionary(page => page.Id, StringComparer.Ordinal);
            var progress = new InlineProgress<StepResult>(step => state.Events.Writer.TryWrite(step));
            var completed = await runner.RunAsync(
                state.Run.Id,
                test,
                pages,
                browser,
                secretValues,
                progress,
                state.Cancellation.Token);
            state.Run = completed with
            {
                Id = state.Run.Id,
                StartedAt = state.Run.StartedAt,
            };
        }
        catch (OperationCanceledException)
        {
            state.Run = state.Run with
            {
                Status = RunStatus.Cancelled,
                CompletedAt = DateTimeOffset.UtcNow,
                Error = "Run cancelled.",
            };
        }
        catch (Exception exception)
        {
            state.Run = state.Run with
            {
                Status = RunStatus.Failed,
                CompletedAt = DateTimeOffset.UtcNow,
                Error = exception.Message,
            };
        }
        finally
        {
            await repository.SaveRunAsync(state.Run, CancellationToken.None);
            state.Events.Writer.TryComplete();
            _runGate.Release();
        }
    }

    private sealed class RunState(TestRun run)
    {
        public TestRun Run { get; set; } = run;
        public Channel<StepResult> Events { get; } = Channel.CreateUnbounded<StepResult>();
        public CancellationTokenSource Cancellation { get; } = new();
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
