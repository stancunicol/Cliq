using System;
using System.Collections.Generic;
using System.Text.Json;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Api
{
    public sealed class OpenWorkspaceRequest
    {
        public OpenWorkspaceRequest(string rootPath) => RootPath = rootPath;
        public string RootPath { get; }
    }

    public sealed class ScanPageRequest
    {
        public ScanPageRequest(string url, string name, BrowserKind? browser = null, bool headless = true)
        {
            Url = url;
            Name = name;
            Browser = browser;
            Headless = headless;
        }

        public string Url { get; }
        public string Name { get; }
        public BrowserKind? Browser { get; }
        public bool Headless { get; }
    }

    public sealed class ScanPageResponse
    {
        public PageDefinition Page { get; set; } = null!;
        public ScanReconciliationReport Reconciliation { get; set; } = null!;
        public LocatorDiffReport? LocatorDiff { get; set; }
        public IReadOnlyList<DynamicElementSuggestion>? DynamicElementSuggestions { get; set; }
        public string? ScreenshotPath { get; set; }
    }

    public sealed class ScanReviewDecisionRequest
    {
        public ScanReviewDecisionRequest(string itemId, ScanReviewDecision decision)
        {
            ItemId = itemId;
            Decision = decision;
        }

        public string ItemId { get; }
        public ScanReviewDecision Decision { get; }
    }

    public sealed class UpdateScanReviewDecisionsRequest
    {
        public UpdateScanReviewDecisionsRequest(IReadOnlyList<ScanReviewDecisionRequest> decisions)
        {
            Decisions = decisions;
        }

        public IReadOnlyList<ScanReviewDecisionRequest> Decisions { get; }
    }


    public sealed class GenerationRequest
    {
        public GenerationRequest(
            IReadOnlyList<string>? pageIds = null,
            IReadOnlyList<string>? testIds = null,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodResolutions = null,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodDiscards = null)
        {
            PageIds = pageIds;
            TestIds = testIds;
            CustomMethodResolutions = customMethodResolutions;
            CustomMethodDiscards = customMethodDiscards;
        }

        public IReadOnlyList<string>? PageIds { get; }
        public IReadOnlyList<string>? TestIds { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>>? CustomMethodResolutions { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>>? CustomMethodDiscards { get; }
    }

    public sealed class StartRunRequest
    {
        public StartRunRequest(BrowserKind? browser = null) => Browser = browser;
        public BrowserKind? Browser { get; }
    }

    public sealed class LocateFromSourceRequest
    {
        public LocateFromSourceRequest(string className, string locatorComment, BrowserKind? browser = null)
        {
            ClassName = className;
            LocatorComment = locatorComment;
            Browser = browser;
        }

        public string ClassName { get; }
        public string LocatorComment { get; }
        public BrowserKind? Browser { get; }
    }

    public sealed class GeneratedPathResponse
    {
        public string Path { get; set; } = null!;
    }

    public sealed class WorkspaceLeaseOwner
    {
        public string? HostKind { get; set; }
        public int ProcessId { get; set; }
        public DateTimeOffset AcquiredAt { get; set; }
    }

    public sealed class CliqProblemDetails
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public string? Code { get; set; }
        public WorkspaceLeaseOwner? Owner { get; set; }
        public Dictionary<string, JsonElement> Extensions { get; } =
            new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RunStreamEvent
    {
        private RunStreamEvent(string eventName, StepResult? step, TestRun? run)
        {
            EventName = eventName;
            Step = step;
            Run = run;
        }

        public string EventName { get; }
        public StepResult? Step { get; }
        public TestRun? Run { get; }
        public bool IsTerminal => Run != null;

        public static RunStreamEvent StepCompleted(StepResult step) =>
            new RunStreamEvent("step", step, null);

        public static RunStreamEvent Done(TestRun run) =>
            new RunStreamEvent("done", null, run);
    }

    public sealed class RunEventStreamDisconnectedException : Exception
    {
        public RunEventStreamDisconnectedException(string runId)
            : base($"The event stream for run '{runId}' disconnected before the done event.")
        {
            RunId = runId;
        }

        public string RunId { get; }
    }

    /// <summary>
    /// The hover-highlight stream (see <see cref="CliqApiClient.StreamHoveredElementAsync"/>) has
    /// no "done" event to wait for - it runs until the caller cancels it - so unlike
    /// <see cref="RunEventStreamDisconnectedException"/> this fires whenever the connection ends
    /// for any reason the caller didn't ask for.
    /// </summary>
    public sealed class HoverStreamDisconnectedException : Exception
    {
        public HoverStreamDisconnectedException(string pageId)
            : base($"The hover-highlight stream for page '{pageId}' disconnected.")
        {
            PageId = pageId;
        }

        public string PageId { get; }
    }
}
