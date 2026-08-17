using System.Reflection;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class WorkspaceConfigurationViewModelTests
{
    [Test]
    public void ProjectEditorValidatesUrlOutputPathsAndNamespaces()
    {
        var editor = new ProjectEditorViewModel(CreateProject());

        editor.Name = string.Empty;
        editor.BaseUrl = "file:///workspace";
        editor.PageObjectsDirectory = "../outside";
        editor.TestsDirectory = @"C:\generated";
        editor.PageObjectsNamespace = "invalid namespace";
        editor.TestsNamespace = "1Invalid";

        Assert.Multiple(() =>
        {
            Assert.That(editor.IsDirty, Is.True);
            Assert.That(editor.Validate(), Has.Count.EqualTo(6));
            Assert.That(editor.ValidationSummary, Does.Contain("Project name is required."));
            Assert.That(editor.ValidationSummary, Does.Contain("absolute HTTP or HTTPS"));
            Assert.That(editor.ValidationSummary, Does.Contain("relative workspace path"));
            Assert.That(editor.ValidationSummary, Does.Contain("valid dotted .NET namespace"));
        });
    }

    [Test]
    public void ProjectEditorRoundTripsAndNormalizesOutputDirectories()
    {
        var editor = new ProjectEditorViewModel(CreateProject())
        {
            Name = "Updated",
            BaseUrl = "https://example.test/app",
            DefaultBrowser = BrowserKind.Edge,
            PageObjectsDirectory = @"Generated\Pages\",
            TestsDirectory = "Generated/Tests/",
            PageObjectsNamespace = "Example.Pages",
            TestsNamespace = "Example.Tests",
        };

        var model = editor.ToModel();
        editor.AcceptChanges(model);

        Assert.Multiple(() =>
        {
            Assert.That(model.Output.PageObjectsDirectory, Is.EqualTo("Generated/Pages"));
            Assert.That(model.Output.TestsDirectory, Is.EqualTo("Generated/Tests"));
            Assert.That(model.DefaultBrowser, Is.EqualTo(BrowserKind.Edge));
            Assert.That(editor.IsDirty, Is.False);
        });
    }

    [Test]
    public void SettingsEditorValidatesRegexStrategiesAndTimeouts()
    {
        var editor = new SettingsEditorViewModel(CreateSettings())
        {
            TestIdAttribute = string.Empty,
            IgnorePatternsText = "[invalid",
            DefaultStepTimeoutText = "0",
            PageReadyTimeoutText = "abc",
            LoadTimeoutText = "-1",
            NetworkIdleGraceText = string.Empty,
        };
        editor.StrategyOrder.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(editor.IsDirty, Is.True);
            Assert.That(editor.ValidationSummary, Does.Contain("Test ID attribute is required."));
            Assert.That(editor.ValidationSummary, Does.Contain("At least one identification strategy"));
            Assert.That(editor.ValidationSummary, Does.Contain("not a valid regular expression"));
            Assert.That(editor.Validate().Count(error => error.Contains("positive whole number")), Is.EqualTo(4));
        });
    }

    [Test]
    public void SettingsEditorSupportsStrategyOrderingAndRoundTrip()
    {
        var editor = new SettingsEditorViewModel(CreateSettings());

        editor.RemoveStrategy("id");
        editor.MoveStrategy("name", -1);
        editor.StrategyToAdd = "id";
        editor.AddSelectedStrategy();
        editor.IgnorePatternsText = "^ember\\d+$\r\n^react-";
        editor.CaptureScreenshotOnFailure = false;
        editor.CaptureScanScreenshot = true;

        var model = editor.ToModel();
        editor.AcceptChanges(model);

        Assert.Multiple(() =>
        {
            Assert.That(model.ElementIdentification.StrategyOrder, Is.EqualTo(new[] { "name", "testId", "id" }));
            Assert.That(model.ElementIdentification.IgnoreIdPatterns, Is.EqualTo(new[] { "^ember\\d+$", "^react-" }));
            Assert.That(model.TestExecution.CaptureScreenshotOnFailure, Is.False);
            Assert.That(model.Scanning.CaptureScreenshot, Is.True);
            Assert.That(editor.IsDirty, Is.False);
        });
    }

    [Test]
    public void SecretEntryNeverExposesPlaintext()
    {
        var entry = new SecretEntryViewModel(new SecretDescriptor("password", true));
        var publicStringProperties = typeof(SecretEntryViewModel)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => (property.Name, Value: (string?)property.GetValue(entry)))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(entry.MaskedValue, Is.EqualTo("••••••••"));
            Assert.That(entry.Status, Is.EqualTo("Configured"));
            Assert.That(publicStringProperties.Select(item => item.Name),
                Is.EqualTo(new[] { "Key", "MaskedValue", "Status" }));
            Assert.That(publicStringProperties.Select(item => item.Value), Does.Not.Contain("actual-password"));
        });
    }

    [Test]
    public async Task WorkbenchLoadsAndSavesConfigurationWithDirtyTracking()
    {
        var store = new ConfigurationStore();
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();

        viewModel.Project!.Name = "Renamed";
        viewModel.Settings!.DefaultStepTimeoutText = "4500";

        Assert.That(viewModel.IsDirty, Is.True);
        Assert.That(await viewModel.SaveAllAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(store.SavedProject?.Name, Is.EqualTo("Renamed"));
            Assert.That(store.SavedSettings?.TestExecution.DefaultStepTimeoutMs, Is.EqualTo(4500));
            Assert.That(viewModel.IsDirty, Is.False);
            Assert.That(viewModel.StatusMessage, Is.EqualTo("Saved all workspace changes."));
        });
    }

    [Test]
    public async Task WorkbenchSecretOperationsValidateAndOnlyExposeDescriptors()
    {
        var store = new ConfigurationStore();
        var viewModel = new WorkbenchViewModel(store);
        await viewModel.InitializeAsync();

        Assert.That(await viewModel.SaveSecretAsync("bad key", "value"), Is.False);
        Assert.That(await viewModel.SaveSecretAsync("account.password", string.Empty), Is.False);
        Assert.That(await viewModel.SaveSecretAsync("account.password", "actual-password"), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(store.LastSecretKey, Is.EqualTo("account.password"));
            Assert.That(store.LastSecretValue, Is.EqualTo("actual-password"));
            Assert.That(viewModel.Secrets, Has.Count.EqualTo(1));
            Assert.That(viewModel.Secrets[0].MaskedValue, Is.EqualTo("••••••••"));
        });

        Assert.That(await viewModel.DeleteSelectedSecretAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(store.DeletedSecretKeys, Is.EqualTo(new[] { "account.password" }));
            Assert.That(viewModel.Secrets, Is.Empty);
        });
    }

    private static ProjectManifest CreateProject()
    {
        return new ProjectManifest(
            ProjectManifest.CurrentSchemaVersion,
            "project",
            "Example",
            "https://example.test",
            BrowserKind.Chrome,
            new OutputSettings(),
            DateTimeOffset.UtcNow);
    }

    private static CliqSettings CreateSettings()
    {
        return new CliqSettings(
            CliqSettings.CurrentSchemaVersion,
            new ElementIdentificationSettings(
                "data-testid",
                new[] { "testId", "id", "name" },
                new[] { "^generated-" }),
            new TestExecutionSettings(10000, 30000, true),
            new ScanningSettings(30000, 1000, false));
    }

    private sealed class ConfigurationStore : FakeWorkbenchStore, IWorkspaceConfigurationStore
    {
        public ProjectManifest? SavedProject { get; private set; }

        public CliqSettings? SavedSettings { get; private set; }

        public string? LastSecretKey { get; private set; }

        public string? LastSecretValue { get; private set; }

        public List<string> DeletedSecretKeys { get; } = new();

        public override Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new WorkspaceSnapshot(
                    @"C:\repo",
                    CreateProject(),
                    Array.Empty<PageDefinition>(),
                    Array.Empty<TestCase>()));
        }

        public override Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken)
        {
            return Task.FromResult(test);
        }

        public override Task DeleteTestAsync(string testId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken)
        {
            return Task.FromResult(page);
        }

        public Task<CliqSettings> LoadSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateSettings());
        }

        public Task<IReadOnlyList<SecretDescriptor>> LoadSecretsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SecretDescriptor>>(Array.Empty<SecretDescriptor>());
        }

        public Task<ProjectManifest> SaveProjectAsync(
            ProjectManifest project,
            CancellationToken cancellationToken)
        {
            SavedProject = project;
            return Task.FromResult(project);
        }

        public Task<CliqSettings> SaveSettingsAsync(
            CliqSettings settings,
            CancellationToken cancellationToken)
        {
            SavedSettings = settings;
            return Task.FromResult(settings);
        }

        public Task SaveSecretAsync(
            string key,
            string plaintextValue,
            CancellationToken cancellationToken)
        {
            LastSecretKey = key;
            LastSecretValue = plaintextValue;
            return Task.CompletedTask;
        }

        public Task DeleteSecretAsync(string key, CancellationToken cancellationToken)
        {
            DeletedSecretKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
