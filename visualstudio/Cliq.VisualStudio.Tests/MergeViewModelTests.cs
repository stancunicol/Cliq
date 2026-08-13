using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class MergeViewModelTests
{
    [Test]
    public void LoadPendingMerges_SyntaxErrorCandidate_DefaultsToDefer()
    {
        var viewModel = new MergeViewModel(new RecordingActionStore());
        var merge = new PendingCustomMethodMerge(
            "login",
            "LoginPage",
            [
                new CustomMethodCandidate("public void Clean()", "public void Clean() { }", false, []),
                new CustomMethodCandidate("public void Broken()", "public void Broken() {", true, ["unexpected token"]),
            ]);

        viewModel.LoadPendingMerges([merge]);
        var page = viewModel.PendingPages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasPendingPages, Is.True);
            Assert.That(page.Candidates[0].Decision, Is.EqualTo(CustomMethodDecision.CarryOver));
            Assert.That(page.Candidates[1].Decision, Is.EqualTo(CustomMethodDecision.Defer));
        });
    }

    [Test]
    public async Task ResolveAsync_SendsOnlyCarryOverMethodsAndClearsPendingPages()
    {
        var store = new RecordingActionStore();
        var viewModel = new MergeViewModel(store);
        var merge = new PendingCustomMethodMerge(
            "login",
            "LoginPage",
            [
                new CustomMethodCandidate("public void Keep()", "public void Keep() { }", false, []),
                new CustomMethodCandidate("public void Skip()", "public void Skip() { }", false, []),
            ]);
        viewModel.LoadPendingMerges([merge]);
        viewModel.PendingPages.Single().Candidates.Single(c => c.Signature == "public void Skip()").Decision =
            CustomMethodDecision.Defer;

        var resolved = await viewModel.ResolveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True);
            Assert.That(viewModel.HasPendingPages, Is.False);
            Assert.That(store.LastGenerationRequest, Is.Not.Null);
            Assert.That(store.LastGenerationRequest!.CustomMethodResolutions, Is.Not.Null);
            var resolutions = store.LastGenerationRequest.CustomMethodResolutions!;
            Assert.That(resolutions["login"], Is.EqualTo(new[] { "public void Keep() { }" }));
        });
    }

    [Test]
    public void CustomMethodCandidateViewModel_SettingIsDiscardTrue_UpdatesDecisionAndOtherFlags()
    {
        var candidate = new CustomMethodCandidateViewModel(
            new CustomMethodCandidate("sig", "public void X() { }", false, []));

        candidate.IsDiscard = true;

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Decision, Is.EqualTo(CustomMethodDecision.Discard));
            Assert.That(candidate.IsCarryOver, Is.False);
            Assert.That(candidate.IsDefer, Is.False);
            Assert.That(candidate.IsDiscard, Is.True);
        });
    }

    [Test]
    public void CustomMethodCandidateViewModel_SettingIsCarryOverTrueOnASyntaxErrorCandidate_IsRefused()
    {
        // Regression: a candidate the extractor couldn't cleanly recover can have a SourceText
        // that isn't really this method's own text (e.g. it borrowed the enclosing class's own
        // closing brace) - carrying it over would re-inject that corrupted text, so this must stay
        // impossible from the view model itself, not just from a disabled control in the UI.
        var candidate = new CustomMethodCandidateViewModel(
            new CustomMethodCandidate("sig", "public void Broken() {", true, ["unexpected token"]));

        candidate.IsCarryOver = true;

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Decision, Is.EqualTo(CustomMethodDecision.Defer));
            Assert.That(candidate.IsCarryOver, Is.False);
            Assert.That(candidate.CanCarryOver, Is.False);
        });
    }

    [Test]
    public async Task ResolveAsync_DiscardedCandidate_IsNeitherCarriedOverNorSentAsAResolution()
    {
        var store = new RecordingActionStore();
        var viewModel = new MergeViewModel(store);
        var merge = new PendingCustomMethodMerge(
            "login",
            "LoginPage",
            [
                new CustomMethodCandidate("public void Keep()", "public void Keep() { }", false, []),
                new CustomMethodCandidate("public void DropMe()", "public void DropMe() { }", false, []),
            ]);
        viewModel.LoadPendingMerges([merge]);
        viewModel.PendingPages.Single().Candidates.Single(c => c.Signature == "public void DropMe()").Decision =
            CustomMethodDecision.Discard;

        var resolved = await viewModel.ResolveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True);
            Assert.That(store.LastGenerationRequest, Is.Not.Null);
            Assert.That(store.LastGenerationRequest!.CustomMethodDiscards, Is.Not.Null);
            var resolutions = store.LastGenerationRequest.CustomMethodResolutions!;
            var discards = store.LastGenerationRequest.CustomMethodDiscards!;
            Assert.That(resolutions["login"], Is.EqualTo(new[] { "public void Keep() { }" }));
            Assert.That(discards["login"], Is.EqualTo(new[] { "public void DropMe() { }" }));
        });
    }

    [Test]
    public async Task DiscardAsync_RemovesEntryFromWaitingListAndCallsStore()
    {
        var entry = new PendingCustomMethodEntry(
            "entry-1", "login", "LoginPage", "public void Old()", "public void Old() { }", false, DateTimeOffset.UtcNow);
        var store = new RecordingActionStore { PendingEntries = [entry] };
        var viewModel = new MergeViewModel(store);
        await viewModel.RefreshWaitingListAsync();

        var removed = await viewModel.DiscardAsync(viewModel.WaitingList.Single());

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(viewModel.WaitingList, Is.Empty);
            Assert.That(store.DeletedPendingIds, Is.EqualTo(new[] { "entry-1" }));
        });
    }

    [Test]
    public async Task IncludeNowAsync_RegeneratesWithThatEntryCarriedOverAndRemovesItFromTheList()
    {
        var entry = new PendingCustomMethodEntry(
            "entry-1", "login", "LoginPage", "public void Old()", "public void Old() { }", false, DateTimeOffset.UtcNow);
        var store = new RecordingActionStore { PendingEntries = [entry] };
        var viewModel = new MergeViewModel(store);
        await viewModel.RefreshWaitingListAsync();

        var included = await viewModel.IncludeNowAsync(viewModel.WaitingList.Single());

        Assert.Multiple(() =>
        {
            Assert.That(included, Is.True);
            Assert.That(viewModel.WaitingList, Is.Empty);
            Assert.That(store.LastGenerationRequest!.PageIds, Is.EqualTo(new[] { "login" }));
            Assert.That(store.LastGenerationRequest.CustomMethodResolutions!["login"], Is.EqualTo(new[] { "public void Old() { }" }));
        });
    }

    [Test]
    public async Task IncludeNowAsync_SyntaxErrorEntry_IsRefusedAndLeftInTheWaitingList()
    {
        // Regression: entry.Model.SourceText is a snapshot from whenever the entry was deferred -
        // even if the user fixed the method in the actual file afterward, including it here would
        // still re-inject that old, corrupted snapshot rather than a fresh extraction.
        var entry = new PendingCustomMethodEntry(
            "entry-1", "login", "LoginPage", "public void Broken()", "public void Broken() {", true, DateTimeOffset.UtcNow);
        var store = new RecordingActionStore { PendingEntries = [entry] };
        var viewModel = new MergeViewModel(store);
        await viewModel.RefreshWaitingListAsync();

        var included = await viewModel.IncludeNowAsync(viewModel.WaitingList.Single());

        Assert.Multiple(() =>
        {
            Assert.That(included, Is.False);
            Assert.That(viewModel.WaitingList, Has.Count.EqualTo(1), "a refused inclusion must not remove the entry");
            Assert.That(store.LastGenerationRequest, Is.Null);
            Assert.That(viewModel.ErrorMessage, Does.Contain("Broken"));
        });
    }

    private sealed class RecordingActionStore : IWorkspaceActionStore
    {
        public IReadOnlyList<PendingCustomMethodEntry> PendingEntries { get; set; } = [];

        public List<string> DeletedPendingIds { get; } = [];

        public GenerationRequest? LastGenerationRequest { get; private set; }

        public Task<ScanPageResponse> ScanAsync(ScanPageRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GenerationResult> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
        {
            LastGenerationRequest = request;
            return Task.FromResult(
                new GenerationResult(Array.Empty<GeneratedArtifact>(), Array.Empty<CustomMethodRecoveryWarning>(), Array.Empty<PendingCustomMethodMerge>()));
        }

        public Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(PendingEntries);

        public Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken)
        {
            DeletedPendingIds.Add(id);
            PendingEntries = PendingEntries.Where(item => item.Id != id).ToArray();
            return Task.CompletedTask;
        }

        public Task LocateElementAsync(
            string pageId,
            string elementId,
            StartRunRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task LocateElementFromSourceAsync(
            LocateFromSourceRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
