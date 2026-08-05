using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;

namespace Cliq.VisualStudio.Core.Runs;

public interface IRunStore
{
    Task<TestRun> StartAsync(string testId, BrowserKind browser, CancellationToken cancellationToken);
    Task StreamAsync(
        string runId,
        Func<RunStreamEvent, Task> onEvent,
        CancellationToken cancellationToken);
    Task<TestRun> GetAsync(string runId, CancellationToken cancellationToken);
    Task CancelAsync(string runId, CancellationToken cancellationToken);
    Task<byte[]> GetScreenshotAsync(
        string runId,
        string fileName,
        CancellationToken cancellationToken);
}

public sealed class RunStepViewModel : BindableBase
{
    private StepResult? _result;
    private bool _isCurrent;

    public RunStepViewModel(TestStep step)
    {
        Step = step;
    }

    public TestStep Step { get; }
    public string StepId => Step.Id;
    public string Label => Step.Label;
    public string Action => Step.Action.ToString();

    public StepResult? Result
    {
        get => _result;
        private set
        {
            if (SetProperty(ref _result, value))
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(Timing));
                OnPropertyChanged(nameof(Error));
                OnPropertyChanged(nameof(IsSelfHealed));
                OnPropertyChanged(nameof(ScreenshotPath));
                OnPropertyChanged(nameof(HasScreenshotPath));
            }
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (SetProperty(ref _isCurrent, value))
            {
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public string Status => IsCurrent
        ? "Running"
        : Result?.Status.ToString() ?? "Waiting";

    public string Duration => Result is null ? string.Empty : $"{Result.DurationMs} ms";

    public string Timing
    {
        get
        {
            if (Result is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (Result.LocateMs.HasValue)
            {
                parts.Add($"locate {Result.LocateMs.Value} ms");
            }

            if (Result.ActionMs.HasValue)
            {
                parts.Add($"action {Result.ActionMs.Value} ms");
            }

            return string.Join(" · ", parts);
        }
    }

    public string? Error => Result?.Error;
    public bool IsSelfHealed => Result?.SelfHealed == true;
    public string? ScreenshotPath => Result?.ScreenshotPath;
    public bool HasScreenshotPath => !string.IsNullOrWhiteSpace(ScreenshotPath);

    public void Apply(StepResult result)
    {
        if (!string.Equals(result.StepId, StepId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Step result ID does not match.", nameof(result));
        }

        Result = result;
        IsCurrent = false;
    }
}

public sealed class RunResultsViewModel : BindableBase, IDisposable
{
    private readonly IRunStore _store;
    private CancellationTokenSource? _runCancellation;
    private TestRun? _run;
    private string? _testName;
    private bool _isRunning;
    private string? _statusMessage;
    private string? _errorMessage;
    private byte[]? _screenshot;
    private string? _screenshotLabel;
    private bool _disposed;

    public RunResultsViewModel(IRunStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ObservableCollection<RunStepViewModel> Steps { get; } = new();

    public TestRun? Run
    {
        get => _run;
        private set
        {
            if (SetProperty(ref _run, value))
            {
                OnPropertyChanged(nameof(RunId));
                OnPropertyChanged(nameof(RunStatusText));
            }
        }
    }

    public string? RunId => Run?.Id;
    public string RunStatusText => Run?.Status.ToString() ?? "Not started";

    public string? TestName
    {
        get => _testName;
        private set => SetProperty(ref _testName, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public byte[]? Screenshot
    {
        get => _screenshot;
        private set
        {
            if (SetProperty(ref _screenshot, value))
            {
                OnPropertyChanged(nameof(HasScreenshot));
            }
        }
    }

    public bool HasScreenshot => Screenshot is { Length: > 0 };

    public string? ScreenshotLabel
    {
        get => _screenshotLabel;
        private set => SetProperty(ref _screenshotLabel, value);
    }

    public async Task StartAsync(
        TestCase test,
        BrowserKind browser,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (IsRunning)
        {
            throw new InvalidOperationException("A Cliq run is already active.");
        }

        var enabledSteps = test.Steps.Where(step => step.Enabled).ToArray();
        if (enabledSteps.Length == 0)
        {
            throw new InvalidOperationException("The selected test has no enabled steps.");
        }

        Reset(test, enabledSteps);
        _runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runToken = _runCancellation.Token;
        IsRunning = true;
        ErrorMessage = null;
        try
        {
            Run = await _store.StartAsync(test.Id, browser, runToken);
            MarkNextCurrent();
            StatusMessage = $"Running {test.Name}…";
            try
            {
                await _store.StreamAsync(
                    Run.Id,
                    ApplyEventAsync,
                    runToken);
            }
            catch (RunEventStreamDisconnectedException)
            {
                StatusMessage = "Live events disconnected; polling run status.";
                await PollUntilCompleteAsync(Run.Id, runToken);
            }
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            StatusMessage = "Run cancellation requested.";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsRunning = false;
            ClearCurrent();
            _runCancellation?.Dispose();
            _runCancellation = null;
        }
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || Run is null)
        {
            return;
        }

        await _store.CancelAsync(Run.Id, cancellationToken);
        StatusMessage = "Cancellation requested…";
    }

    public async Task LoadScreenshotAsync(
        RunStepViewModel step,
        CancellationToken cancellationToken = default)
    {
        if (Run is null || string.IsNullOrWhiteSpace(step.ScreenshotPath))
        {
            Screenshot = null;
            ScreenshotLabel = null;
            return;
        }

        Screenshot = await _store.GetScreenshotAsync(
            Run.Id,
            step.ScreenshotPath!,
            cancellationToken);
        ScreenshotLabel = $"Failure screenshot — {step.Label}";
    }

    public void CloseScreenshot()
    {
        Screenshot = null;
        ScreenshotLabel = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runCancellation?.Cancel();
        _runCancellation?.Dispose();
        _runCancellation = null;
    }

    private Task ApplyEventAsync(RunStreamEvent runEvent)
    {
        if (runEvent.Step is not null)
        {
            ApplyStep(runEvent.Step);
        }

        if (runEvent.Run is not null)
        {
            ApplyRun(runEvent.Run);
        }

        return Task.CompletedTask;
    }

    private async Task PollUntilCompleteAsync(string runId, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await _store.GetAsync(runId, cancellationToken);
            ApplyRun(current);
            if (IsTerminal(current.Status))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private void ApplyRun(TestRun run)
    {
        Run = run;
        foreach (var result in run.Steps)
        {
            ApplyStep(result);
        }

        if (IsTerminal(run.Status))
        {
            ClearCurrent();
            StatusMessage = run.Status == RunStatus.Passed
                ? "All steps passed."
                : run.Error ?? $"Run {run.Status.ToString().ToLowerInvariant()}.";
        }
    }

    private void ApplyStep(StepResult result)
    {
        var step = Steps.FirstOrDefault(candidate => candidate.StepId == result.StepId);
        step?.Apply(result);
        MarkNextCurrent();
    }

    private void Reset(TestCase test, IEnumerable<TestStep> enabledSteps)
    {
        Run = null;
        TestName = test.Name;
        StatusMessage = "Preparing the browser…";
        ErrorMessage = null;
        Screenshot = null;
        ScreenshotLabel = null;
        Steps.Clear();
        foreach (var step in enabledSteps)
        {
            Steps.Add(new RunStepViewModel(step));
        }
    }

    private void MarkNextCurrent()
    {
        var next = Steps.FirstOrDefault(step => step.Result is null);
        foreach (var step in Steps)
        {
            step.IsCurrent = ReferenceEquals(step, next);
        }
    }

    private void ClearCurrent()
    {
        foreach (var step in Steps)
        {
            step.IsCurrent = false;
        }
    }

    private static bool IsTerminal(RunStatus status) =>
        status == RunStatus.Passed ||
        status == RunStatus.Failed ||
        status == RunStatus.Cancelled;

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RunResultsViewModel));
        }
    }
}
