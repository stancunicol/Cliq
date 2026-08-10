using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Runs;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class RunResultsViewModelTests
{
    [Test]
    public async Task StartStreamsStepsAndTerminalRun()
    {
        var test = TestWithTwoSteps();
        var store = new FakeRunStore();
        var viewModel = new RunResultsViewModel(store);

        await viewModel.StartAsync(test, BrowserKind.Chrome);

        Assert.That(viewModel.RunStatusText, Is.EqualTo("Passed"));
        Assert.That(viewModel.Steps.Select(step => step.Status), Is.All.EqualTo("Passed"));
        Assert.That(viewModel.StatusMessage, Is.EqualTo("All steps passed."));
        Assert.That(viewModel.IsRunning, Is.False);
    }

    [Test]
    public async Task DisconnectedStreamFallsBackToPolling()
    {
        var store = new FakeRunStore { DisconnectStream = true };
        var viewModel = new RunResultsViewModel(store);

        await viewModel.StartAsync(TestWithTwoSteps(), BrowserKind.Edge);

        Assert.That(store.GetCount, Is.EqualTo(1));
        Assert.That(viewModel.RunStatusText, Is.EqualTo("Passed"));
    }

    [Test]
    public async Task CancelRequestsBackendCancellation()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new FakeRunStore { StreamGate = gate.Task };
        var viewModel = new RunResultsViewModel(store);
        var runTask = viewModel.StartAsync(TestWithTwoSteps(), BrowserKind.Chrome);
        await store.Started.Task;

        await viewModel.CancelAsync();
        gate.SetResult(true);
        await runTask;

        Assert.That(store.CancelledRunId, Is.EqualTo("run-1"));
    }

    [Test]
    public async Task ScreenshotIsLoadedAndCanBeCleared()
    {
        var store = new FakeRunStore();
        var viewModel = new RunResultsViewModel(store);
        await viewModel.StartAsync(TestWithTwoSteps(), BrowserKind.Chrome);
        var step = viewModel.Steps[0];
        step.Apply(new StepResult(
            step.StepId,
            RunStatus.Failed,
            10,
            ScreenshotPath: "failure.png"));

        Assert.That(step.HasScreenshotPath, Is.True);

        await viewModel.LoadScreenshotAsync(step);
        Assert.That(viewModel.Screenshot, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(viewModel.HasScreenshot, Is.True);

        viewModel.CloseScreenshot();
        Assert.That(viewModel.HasScreenshot, Is.False);
    }

    [Test]
    public void EmptyTestCannotRun()
    {
        var test = new TestCase(
            1,
            "empty",
            "Empty",
            null,
            Array.Empty<string>(),
            Array.Empty<TestStep>(),
            DateTimeOffset.UtcNow);
        var viewModel = new RunResultsViewModel(new FakeRunStore());

        Assert.That(
            async () => await viewModel.StartAsync(test, BrowserKind.Chrome),
            Throws.TypeOf<InvalidOperationException>());
    }

    private static TestCase TestWithTwoSteps() =>
        new(
            1,
            "checkout",
            "Checkout",
            null,
            Array.Empty<string>(),
            new[]
            {
                new TestStep("step-1", StepAction.Navigate, "Open", true, Url: "https://example.test"),
                new TestStep("step-2", StepAction.AssertUrl, "Check", true, Value: StepValue.Literal("example")),
            },
            DateTimeOffset.UtcNow);

    private sealed class FakeRunStore : IRunStore
    {
        public bool DisconnectStream { get; set; }
        public Task? StreamGate { get; set; }
        public TaskCompletionSource<bool> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? CancelledRunId { get; private set; }
        public int GetCount { get; private set; }

        public Task<TestRun> StartAsync(
            string testId,
            BrowserKind browser,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult(true);
            return Task.FromResult(Run(RunStatus.Queued, Array.Empty<StepResult>()));
        }

        public async Task StreamAsync(
            string runId,
            Func<RunStreamEvent, Task> onEvent,
            CancellationToken cancellationToken)
        {
            if (StreamGate is not null)
            {
                await StreamGate;
            }

            if (DisconnectStream)
            {
                throw new RunEventStreamDisconnectedException(runId);
            }

            var first = new StepResult("step-1", RunStatus.Passed, 20);
            var second = new StepResult("step-2", RunStatus.Passed, 10);
            await onEvent(RunStreamEvent.StepCompleted(first));
            await onEvent(RunStreamEvent.StepCompleted(second));
            await onEvent(RunStreamEvent.Done(Run(RunStatus.Passed, new[] { first, second })));
        }

        public Task<TestRun> GetAsync(string runId, CancellationToken cancellationToken)
        {
            GetCount++;
            return Task.FromResult(Run(
                RunStatus.Passed,
                new[]
                {
                    new StepResult("step-1", RunStatus.Passed, 20),
                    new StepResult("step-2", RunStatus.Passed, 10),
                }));
        }

        public Task CancelAsync(string runId, CancellationToken cancellationToken)
        {
            CancelledRunId = runId;
            return Task.CompletedTask;
        }

        public Task<byte[]> GetScreenshotAsync(
            string runId,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult(new byte[] { 1, 2, 3 });

        private static TestRun Run(RunStatus status, IReadOnlyList<StepResult> steps) =>
            new(
                "run-1",
                "checkout",
                status,
                DateTimeOffset.UtcNow,
                status == RunStatus.Queued ? null : DateTimeOffset.UtcNow,
                steps);
    }
}
