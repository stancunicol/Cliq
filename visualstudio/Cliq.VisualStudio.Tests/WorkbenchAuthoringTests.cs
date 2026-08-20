using Cliq.Domain;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class WorkbenchAuthoringTests
{
    [Test]
    public async Task InitializeAndSearchExposeTestsAndPages()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);

        await viewModel.InitializeAsync();
        viewModel.SearchText = "checkout";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Tests, Has.Count.EqualTo(2));
            Assert.That(viewModel.Pages, Has.Count.EqualTo(1));
            Assert.That(viewModel.FilteredTests.Select(test => test.Name), Is.EqualTo(new[] { "Checkout" }));
            Assert.That(viewModel.FilteredPages.Select(page => page.Name), Is.EqualTo(new[] { "Checkout page" }));
            Assert.That(viewModel.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task SearchMatchesTestTags()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);

        await viewModel.InitializeAsync();
        viewModel.SearchText = "smoke";

        Assert.That(viewModel.FilteredTests.Select(test => test.Name), Is.EqualTo(new[] { "Checkout" }));

        viewModel.SearchText = "SMOKE";
        Assert.That(viewModel.FilteredTests.Select(test => test.Name), Is.EqualTo(new[] { "Checkout" }));
    }

    [Test]
    public async Task SelectingTestsAndPagesIsMutuallyExclusive()
    {
        var viewModel = new WorkbenchViewModel(new RecordingWorkbenchStore(CreateSnapshot()));
        await viewModel.InitializeAsync();

        Assert.That(viewModel.SelectedTest, Is.Not.Null);
        Assert.That(viewModel.SelectedPage, Is.Null);

        viewModel.SelectedPage = viewModel.Pages[0];
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedPage, Is.SameAs(viewModel.Pages[0]));
            Assert.That(viewModel.SelectedTest, Is.Null);
            Assert.That(viewModel.HasSelectedTest, Is.False);
        });

        viewModel.SelectedTest = viewModel.Tests[1];
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedTest, Is.SameAs(viewModel.Tests[1]));
            Assert.That(viewModel.SelectedPage, Is.Null);
        });
    }

    [Test]
    public async Task CreateEditSaveAndDeleteNewTestTracksDirtyState()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();

        viewModel.CreateTest();
        var test = viewModel.SelectedTest!;
        test.Name = "Account settings";
        test.Description = "Updates a profile";
        test.TagsText = "profile, smoke";
        test.AddStep();
        test.SelectedStep!.Url = "https://example.test/account";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsDirty, Is.True);
            Assert.That(test.IsNew, Is.True);
            Assert.That(test.ToModel().Tags, Is.EqualTo(new[] { "profile", "smoke" }));
        });

        Assert.That(await viewModel.SaveSelectedAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(store.SavedTests, Has.Count.EqualTo(1));
            Assert.That(test.IsDirty, Is.False);
            Assert.That(test.IsNew, Is.False);
        });

        viewModel.CreateTest();
        Assert.That(await viewModel.DeleteSelectedTestAsync(), Is.True);
        Assert.That(store.DeletedTestIds, Is.Empty, "Unsaved tests are removed locally.");
    }

    [Test]
    public void StepActionsNormalizeFieldsAndValidateActionSpecificInput()
    {
        var pages = CreateSnapshot().Pages;
        var step = new TestStepEditorViewModel(
            new TestStep("step", StepAction.Navigate, "Navigate", true, Url: "bad-url"),
            () => pages);

        Assert.That(step.Validate(), Does.Contain("Navigate URL must be an absolute HTTP or HTTPS URL."));

        step.Action = StepAction.Fill;
        Assert.Multiple(() =>
        {
            Assert.That(step.PageId, Is.EqualTo("checkout-page"));
            Assert.That(step.ElementId, Is.EqualTo("email"));
            Assert.That(step.RequiresValue, Is.True);
            Assert.That(step.Validate(), Does.Contain("Step value is required."));
        });

        step.ValueKind = "secret";
        step.Value = "account_password";
        step.TimeoutText = "0";
        Assert.That(step.Validate(), Does.Contain("Timeout must be a positive whole number."));

        step.TimeoutText = "2500";
        Assert.That(step.Validate(), Is.Empty);
        Assert.That(step.ToModel().Value, Is.EqualTo(StepValue.Secret("account_password")));
    }

    [Test]
    public void StepEditingSupportsDuplicateDeleteAndReorder()
    {
        var snapshot = CreateSnapshot();
        var editor = new TestEditorViewModel(snapshot.Tests[0], () => snapshot.Pages);
        var originalFirstId = editor.Steps[0].Id;

        editor.SelectedStep = editor.Steps[0];
        editor.DuplicateSelectedStep();
        var duplicateId = editor.SelectedStep!.Id;
        editor.MoveSelectedStep(1);

        Assert.Multiple(() =>
        {
            Assert.That(editor.Steps, Has.Count.EqualTo(3));
            Assert.That(duplicateId, Is.Not.EqualTo(originalFirstId));
            Assert.That(editor.Steps[2].Id, Is.EqualTo(duplicateId));
            Assert.That(editor.IsDirty, Is.True);
        });

        editor.DeleteSelectedStep();
        Assert.That(editor.Steps, Has.Count.EqualTo(2));
    }

    [Test]
    public void StepCommandStateTracksSelectionAndPosition()
    {
        var snapshot = CreateSnapshot();
        var editor = new TestEditorViewModel(snapshot.Tests[0], () => snapshot.Pages);

        editor.SelectedStep = editor.Steps[0];
        Assert.Multiple(() =>
        {
            Assert.That(editor.HasSelectedStep, Is.True);
            Assert.That(editor.CanMoveSelectedStepUp, Is.False);
            Assert.That(editor.CanMoveSelectedStepDown, Is.True);
        });

        editor.SelectedStep = editor.Steps[1];
        Assert.Multiple(() =>
        {
            Assert.That(editor.CanMoveSelectedStepUp, Is.True);
            Assert.That(editor.CanMoveSelectedStepDown, Is.False);
        });
    }

    [TestCase("api_token", true)]
    [TestCase("API.token-2", true)]
    [TestCase("2fa-token", false)]
    [TestCase("contains space", false)]
    [TestCase("", false)]
    public void SecretKeyValidationMatchesEditorRules(string key, bool expected)
    {
        Assert.That(WorkbenchViewModel.IsValidSecretKey(key), Is.EqualTo(expected));
    }

    [Test]
    public async Task InvalidTestIsNotSavedAndShowsAllValidationErrors()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();
        viewModel.CreateTest();
        viewModel.SelectedTest!.Name = string.Empty;
        viewModel.SelectedTest.AddStep();
        viewModel.SelectedTest.SelectedStep!.Url = "file:///tmp/test";

        Assert.That(await viewModel.SaveSelectedAsync(), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(store.SavedTests, Is.Empty);
            Assert.That(viewModel.ErrorMessage, Does.Contain("Test name is required."));
            Assert.That(viewModel.ErrorMessage, Does.Contain("Navigate URL"));
        });
    }

    [Test]
    public async Task SaveAllPersistsOnlyDirtyTests()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();
        viewModel.Tests[1].Description = "Changed";

        Assert.That(await viewModel.SaveAllAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(store.SavedTests.Select(test => test.Id), Is.EqualTo(new[] { "login-test" }));
            Assert.That(viewModel.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task ReloadHonorsDirtyStatePrompt()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var prompt = new RecordingPrompt { DiscardResult = false };
        var viewModel = new WorkbenchViewModel(store, prompt);
        await viewModel.InitializeAsync();
        viewModel.SelectedTest!.Name = "Changed locally";

        Assert.That(await viewModel.ReloadAsync(), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(prompt.DiscardCalls, Is.EqualTo(1));
            Assert.That(store.LoadCalls, Is.EqualTo(1));
            Assert.That(viewModel.SelectedTest.Name, Is.EqualTo("Changed locally"));
        });

        prompt.DiscardResult = true;
        Assert.That(await viewModel.ReloadAsync(), Is.True);
        Assert.That(store.LoadCalls, Is.EqualTo(2));
    }

    [Test]
    public void PageInspectorShowsLocatorAndMissingStatus()
    {
        var page = CreateSnapshot().Pages[0];
        var inspector = new PageEditorViewModel(page);

        Assert.Multiple(() =>
        {
            Assert.That(inspector.Elements[0].Locator, Is.EqualTo("TestId: email"));
            Assert.That(inspector.Elements[1].Status, Is.EqualTo("Not found on last scan"));
            Assert.That(inspector.Elements[1].NotFound, Is.True);
        });
    }

    [Test]
    public void RemoveNotFoundElements_RemovesFlaggedRowsAndMarksDirty()
    {
        var page = CreateSnapshot().Pages[0];
        var inspector = new PageEditorViewModel(page);

        Assert.That(inspector.HasNotFoundElements, Is.True);
        inspector.RemoveNotFoundElements();

        Assert.Multiple(() =>
        {
            Assert.That(inspector.Elements, Has.Count.EqualTo(1));
            Assert.That(inspector.Elements.Any(element => element.NotFound), Is.False);
            Assert.That(inspector.HasNotFoundElements, Is.False);
            Assert.That(inspector.IsDirty, Is.True);
            Assert.That(inspector.ToModel().Elements, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RemoveNotFoundElements_NoStaleElements_IsANoOp()
    {
        var original = CreateSnapshot().Pages[0];
        var page = original with { Elements = new[] { original.Elements[0] } };
        var inspector = new PageEditorViewModel(page);

        inspector.RemoveNotFoundElements();

        Assert.Multiple(() =>
        {
            Assert.That(inspector.Elements, Has.Count.EqualTo(1));
            Assert.That(inspector.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task RenamingSelectedPageTracksDirtyStateAndSaves()
    {
        var store = new RecordingWorkbenchStore(CreateSnapshot());
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();

        viewModel.SelectedPage = viewModel.Pages[0];
        Assert.That(viewModel.CanSaveSelected, Is.False);

        viewModel.SelectedPage.Name = "Renamed checkout page";
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedPage.IsDirty, Is.True);
            Assert.That(viewModel.IsDirty, Is.True);
            Assert.That(viewModel.CanSaveSelected, Is.True);
        });

        var saved = await viewModel.SaveSelectedAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(store.SavedPages, Has.Count.EqualTo(1));
            Assert.That(store.SavedPages[0].Name, Is.EqualTo("Renamed checkout page"));
            Assert.That(viewModel.SelectedPage.IsDirty, Is.False);
            Assert.That(viewModel.IsDirty, Is.False);
        });
    }

    private static WorkspaceSnapshot CreateSnapshot()
    {
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "checkout-page",
            "Checkout page",
            "https://example.test/checkout",
            new[]
            {
                new PageElement(
                    "email",
                    "Email",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "email")),
                new PageElement(
                    "submit",
                    "Submit",
                    "button",
                    StepAction.Click,
                    new Locator(LocatorStrategy.Css, "#submit"),
                    NotFound: true),
            },
            DateTimeOffset.UtcNow);
        var checkout = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout-test",
            "Checkout",
            null,
            new[] { "smoke" },
            new[]
            {
                new TestStep("navigate", StepAction.Navigate, "Open checkout", true, Url: page.Url),
                new TestStep(
                    "fill",
                    StepAction.Fill,
                    "Enter email",
                    true,
                    page.Id,
                    "email",
                    Value: StepValue.Literal("person@example.test")),
            },
            DateTimeOffset.UtcNow);
        var login = new TestCase(
            TestCase.CurrentSchemaVersion,
            "login-test",
            "Log in",
            null,
            Array.Empty<string>(),
            Array.Empty<TestStep>(),
            DateTimeOffset.UtcNow);
        return new WorkspaceSnapshot(
            @"C:\repo",
            new ProjectManifest(
                ProjectManifest.CurrentSchemaVersion,
                "project",
                "Example",
                "https://example.test",
                BrowserKind.Chrome,
                new OutputSettings(),
                DateTimeOffset.UtcNow),
            new[] { page },
            new[] { checkout, login });
    }

    private sealed class RecordingWorkbenchStore : FakeWorkbenchStore
    {
        private readonly WorkspaceSnapshot _snapshot;

        public RecordingWorkbenchStore(WorkspaceSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int LoadCalls { get; private set; }

        public List<TestCase> SavedTests { get; } = new();

        public List<string> DeletedTestIds { get; } = new();

        public List<PageDefinition> SavedPages { get; } = new();

        public override Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCalls++;
            return Task.FromResult(_snapshot);
        }

        public override Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken)
        {
            SavedTests.Add(test);
            return Task.FromResult(test);
        }

        public override Task DeleteTestAsync(string testId, CancellationToken cancellationToken)
        {
            DeletedTestIds.Add(testId);
            return Task.CompletedTask;
        }

        public override Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken)
        {
            SavedPages.Add(page);
            return Task.FromResult(page);
        }
    }

    private sealed class RecordingPrompt : IWorkbenchPrompt
    {
        public bool DiscardResult { get; set; } = true;

        public int DiscardCalls { get; private set; }

        public Task<bool> ConfirmDiscardChangesAsync(
            string operation,
            CancellationToken cancellationToken)
        {
            DiscardCalls++;
            return Task.FromResult(DiscardResult);
        }

        public Task<bool> ConfirmDeleteTestAsync(
            string testName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
