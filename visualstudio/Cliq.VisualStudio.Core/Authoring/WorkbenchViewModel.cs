using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class WorkbenchViewModel : BindableBase
{
    private readonly IWorkbenchStore _store;
    private readonly IWorkspaceConfigurationStore? _configurationStore;
    private readonly IWorkbenchPrompt _prompt;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private string _searchText = string.Empty;
    private TestEditorViewModel? _selectedTest;
    private PageEditorViewModel? _selectedPage;
    private bool _isBusy;
    private string? _statusMessage;
    private string? _errorMessage;
    private IReadOnlyList<PageDefinition> _pageModels = Array.Empty<PageDefinition>();
    private ProjectEditorViewModel? _project;
    private SettingsEditorViewModel? _settings;
    private SecretEntryViewModel? _selectedSecret;

    public WorkbenchViewModel(
        IWorkbenchStore store,
        IWorkbenchPrompt? prompt = null,
        IWorkspaceConfigurationStore? configurationStore = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _prompt = prompt ?? new PermissiveWorkbenchPrompt();
        _configurationStore = configurationStore ?? store as IWorkspaceConfigurationStore;
    }

    public ObservableCollection<TestEditorViewModel> Tests { get; } = new();

    public ObservableCollection<PageEditorViewModel> Pages { get; } = new();

    public ObservableCollection<SecretEntryViewModel> Secrets { get; } = new();

    public ProjectEditorViewModel? Project
    {
        get => _project;
        private set => SetProperty(ref _project, value);
    }

    public SettingsEditorViewModel? Settings
    {
        get => _settings;
        private set => SetProperty(ref _settings, value);
    }

    public SecretEntryViewModel? SelectedSecret
    {
        get => _selectedSecret;
        set
        {
            if (SetProperty(ref _selectedSecret, value))
            {
                OnPropertyChanged(nameof(HasSelectedSecret));
            }
        }
    }

    public bool HasSelectedSecret => SelectedSecret != null;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(FilteredTests));
                OnPropertyChanged(nameof(FilteredPages));
            }
        }
    }

    public IEnumerable<TestEditorViewModel> FilteredTests =>
        Tests.Where(test =>
            MatchesSearch(test.Name) ||
            MatchesSearch(test.TagsText));

    public IEnumerable<PageEditorViewModel> FilteredPages =>
        Pages.Where(page =>
            MatchesSearch(page.Name) ||
            MatchesSearch(page.Url));

    public TestEditorViewModel? SelectedTest
    {
        get => _selectedTest;
        set
        {
            if (!SetProperty(ref _selectedTest, value))
            {
                return;
            }

            if (value != null && _selectedPage != null)
            {
                _selectedPage = null;
                OnPropertyChanged(nameof(SelectedPage));
            }

            OnPropertyChanged(nameof(HasSelectedTest));
            OnPropertyChanged(nameof(CanSaveSelected));
        }
    }

    public bool HasSelectedTest => SelectedTest != null;

    public bool CanSaveSelected =>
        IsNotBusy && (SelectedTest?.IsDirty == true || SelectedPage?.IsDirty == true);

    public PageEditorViewModel? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!SetProperty(ref _selectedPage, value))
            {
                return;
            }

            if (value != null && _selectedTest != null)
            {
                _selectedTest = null;
                OnPropertyChanged(nameof(SelectedTest));
                OnPropertyChanged(nameof(HasSelectedTest));
            }

            OnPropertyChanged(nameof(CanSaveSelected));
            OnPropertyChanged(nameof(HasSelectedPage));
        }
    }

    public bool HasSelectedPage => SelectedPage != null;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsNotBusy => !IsBusy;

    public bool CanSaveAll => IsNotBusy && IsDirty;

    public bool IsDirty =>
        Tests.Any(test => test.IsDirty) ||
        Pages.Any(page => page.IsDirty) ||
        Project?.IsDirty == true ||
        Settings?.IsDirty == true;

    public string DirtySummary
    {
        get
        {
            var count = Tests.Count(test => test.IsDirty) +
                        Pages.Count(page => page.IsDirty) +
                        (Project?.IsDirty == true ? 1 : 0) +
                        (Settings?.IsDirty == true ? 1 : 0);
            return count == 0
                ? "All changes saved"
                : $"{count} unsaved item{(count == 1 ? string.Empty : "s")}";
        }
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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RunExclusiveAsync(
            async () =>
            {
                await ReloadCoreAsync(cancellationToken);
                StatusMessage = "Workspace loaded.";
                return true;
            });
    }

    public void CreateTest()
    {
        var model = new TestCase(
            TestCase.CurrentSchemaVersion,
            Guid.NewGuid().ToString("N"),
            "New test",
            null,
            Array.Empty<string>(),
            Array.Empty<TestStep>(),
            DateTimeOffset.UtcNow);
        var editor = CreateTestEditor(model);
        editor.MarkAsNew();
        Tests.Add(editor);
        SelectedTest = editor;
        RaiseCollectionState();
    }

    public async Task<bool> DeleteSelectedTestAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedTest;
        if (selected == null ||
            !await _prompt.ConfirmDeleteTestAsync(selected.DisplayName, cancellationToken))
        {
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                if (!selected.IsNew)
                {
                    await _store.DeleteTestAsync(selected.Id, cancellationToken);
                }
                var index = Tests.IndexOf(selected);
                Unsubscribe(selected);
                Tests.Remove(selected);
                SelectedTest = Tests.Count == 0 ? null : Tests[Math.Min(index, Tests.Count - 1)];
                StatusMessage = $"Deleted {selected.DisplayName}.";
                RaiseCollectionState();
                return true;
            });
    }

    public async Task<bool> DeleteSelectedPageAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedPage;
        if (selected == null)
        {
            return false;
        }

        var references = FindPageReferences(selected.Id);
        if (!await _prompt.ConfirmDeletePageAsync(selected.DisplayName, references, cancellationToken))
        {
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                await _store.DeletePageAsync(selected.Id, cancellationToken);
                var index = Pages.IndexOf(selected);
                Unsubscribe(selected);
                Pages.Remove(selected);
                _pageModels = _pageModels.Where(page => page.Id != selected.Id).ToArray();
                SelectedPage = Pages.Count == 0 ? null : Pages[Math.Min(index, Pages.Count - 1)];
                StatusMessage = $"Deleted {selected.DisplayName}.";
                RaiseCollectionState();
                return true;
            });
    }

    public async Task<bool> DuplicateSelectedPageAsync(CancellationToken cancellationToken = default)
    {
        return await RunExclusiveAsync(
            async () =>
            {
                var selected = SelectedPage;
                if (selected == null)
                {
                    return false;
                }

                var duplicate = selected.ToModel() with
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = $"{selected.DisplayName} copy",
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                var saved = await _store.SavePageAsync(duplicate, cancellationToken);
                var editor = CreatePageEditor(saved);
                Pages.Add(editor);
                _pageModels = _pageModels.Concat(new[] { saved }).ToArray();
                SelectedPage = editor;
                StatusMessage = $"Duplicated {saved.Name}.";
                RaiseCollectionState();
                return true;
            });
    }

    private IReadOnlyList<PageReferenceInfo> FindPageReferences(string pageId)
    {
        return Tests
            .SelectMany(test => test.Steps
                .Where(step => step.PageId == pageId)
                .Select(step => new PageReferenceInfo(test.DisplayName, step.Label)))
            .ToArray();
    }

    public async Task<bool> SaveSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedTest is { } test)
        {
            return await ValidateAndSaveAsync(test.Validate(), async () =>
            {
                var saved = await _store.SaveTestAsync(test.ToModel(), cancellationToken);
                test.AcceptChanges(saved);
                return $"Saved {saved.Name}.";
            });
        }

        if (SelectedPage is { } page)
        {
            return await ValidateAndSaveAsync(page.Validate(), async () =>
            {
                var saved = await _store.SavePageAsync(page.ToModel(), cancellationToken);
                page.AcceptChanges(saved);
                return $"Saved {saved.Name}. Regenerate to rename the Page Object file.";
            });
        }

        return false;
    }

    /// <summary>
    /// The "validate, bail out with ErrorMessage if invalid, otherwise save under
    /// <see cref="RunExclusiveAsync"/> and report the resulting status" shape every single-entity
    /// save (a selected test or page, the project, the settings) needs - what differs per caller is
    /// just which store call to make, how to accept the saved result back into the editor, and what
    /// status text to show, all captured in <paramref name="saveAndDescribe"/>.
    /// </summary>
    private Task<bool> ValidateAndSaveAsync(
        IReadOnlyList<string> validationErrors,
        Func<Task<string>> saveAndDescribe)
    {
        if (validationErrors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, validationErrors);
            return Task.FromResult(false);
        }

        return RunExclusiveAsync(async () =>
        {
            StatusMessage = await saveAndDescribe();
            RaiseDirtyState();
            return true;
        });
    }

    public async Task<bool> SaveAllAsync(CancellationToken cancellationToken = default)
    {
        var dirtyTests = Tests.Where(test => test.IsDirty).ToArray();
        var dirtyPages = Pages.Where(page => page.IsDirty).ToArray();
        var errors = dirtyTests
            .SelectMany(test => test.Validate().Select(error => $"{test.DisplayName}: {error}"))
            .Concat(dirtyPages.SelectMany(page => page.Validate().Select(error => $"{page.DisplayName}: {error}")))
            .Concat(Project?.IsDirty == true
                ? Project.Validate().Select(error => $"Project: {error}")
                : Array.Empty<string>())
            .Concat(Settings?.IsDirty == true
                ? Settings.Validate().Select(error => $"Settings: {error}")
                : Array.Empty<string>())
            .ToArray();
        if (_configurationStore == null &&
            (Project?.IsDirty == true || Settings?.IsDirty == true))
        {
            ErrorMessage = "Workspace configuration storage is not available.";
            return false;
        }

        if (errors.Length > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                var projectWasDirty = Project?.IsDirty == true;
                var settingsWereDirty = Settings?.IsDirty == true;
                foreach (var test in dirtyTests)
                {
                    var saved = await _store.SaveTestAsync(test.ToModel(), cancellationToken);
                    test.AcceptChanges(saved);
                }

                foreach (var page in dirtyPages)
                {
                    var saved = await _store.SavePageAsync(page.ToModel(), cancellationToken);
                    page.AcceptChanges(saved);
                }

                if (Project?.IsDirty == true && _configurationStore != null)
                {
                    var savedProject = await _configurationStore.SaveProjectAsync(
                        Project.ToModel(),
                        cancellationToken);
                    Project.AcceptChanges(savedProject);
                }

                if (Settings?.IsDirty == true && _configurationStore != null)
                {
                    var savedSettings = await _configurationStore.SaveSettingsAsync(
                        Settings.ToModel(),
                        cancellationToken);
                    Settings.AcceptChanges(savedSettings);
                }

                var savedItemCount = dirtyTests.Length + dirtyPages.Length +
                                     (projectWasDirty ? 1 : 0) +
                                     (settingsWereDirty ? 1 : 0);
                StatusMessage = savedItemCount == 0
                    ? "No changes to save."
                    : "Saved all workspace changes.";
                RaiseDirtyState();
                return true;
            });
    }

    public async Task<bool> SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        if (Project == null || _configurationStore == null)
        {
            ErrorMessage = "Project settings are not available until a workspace is connected.";
            return false;
        }

        return await ValidateAndSaveAsync(Project.Validate(), async () =>
        {
            var saved = await _configurationStore.SaveProjectAsync(Project.ToModel(), cancellationToken);
            Project.AcceptChanges(saved);
            return "Project settings saved.";
        });
    }

    public async Task<bool> SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (Settings == null || _configurationStore == null)
        {
            ErrorMessage = "Cliq settings are not available until a workspace is connected.";
            return false;
        }

        return await ValidateAndSaveAsync(Settings.Validate(), async () =>
        {
            var saved = await _configurationStore.SaveSettingsAsync(Settings.ToModel(), cancellationToken);
            Settings.AcceptChanges(saved);
            return "Cliq settings saved.";
        });
    }

    public async Task<bool> SaveSecretAsync(
        string key,
        string plaintextValue,
        CancellationToken cancellationToken = default)
    {
        if (_configurationStore == null)
        {
            ErrorMessage = "Secret storage is not available until a workspace is connected.";
            return false;
        }

        var normalizedKey = key?.Trim() ?? string.Empty;
        if (!IsValidSecretKey(normalizedKey))
        {
            ErrorMessage = "Secret key must start with a letter or underscore and contain only letters, numbers, '.', '-' or '_'.";
            return false;
        }

        if (string.IsNullOrEmpty(plaintextValue))
        {
            ErrorMessage = "Secret value cannot be empty.";
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                await _configurationStore.SaveSecretAsync(
                    normalizedKey,
                    plaintextValue,
                    cancellationToken);
                ReplaceSecret(new SecretDescriptor(normalizedKey, true));
                StatusMessage = $"Secret '{normalizedKey}' saved.";
                return true;
            });
    }

    public static bool IsValidSecretKey(string? key) =>
        Regex.IsMatch(key?.Trim() ?? string.Empty, @"^[A-Za-z_][A-Za-z0-9_.-]*$");

    public async Task<bool> DeleteSelectedSecretAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedSecret;
        if (selected == null || _configurationStore == null)
        {
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                await _configurationStore.DeleteSecretAsync(selected.Key, cancellationToken);
                Secrets.Remove(selected);
                SelectedSecret = Secrets.FirstOrDefault();
                StatusMessage = $"Secret '{selected.Key}' deleted.";
                return true;
            });
    }

    public async Task<bool> ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (IsDirty &&
            !await _prompt.ConfirmDiscardChangesAsync("reload the workspace", cancellationToken))
        {
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                await ReloadCoreAsync(cancellationToken);
                StatusMessage = "Workspace reloaded.";
                return true;
            });
    }

    private async Task ReloadCoreAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _store.LoadAsync(cancellationToken);
        var settings = _configurationStore == null
            ? CreateDefaultSettings()
            : await _configurationStore.LoadSettingsAsync(cancellationToken);
        var secrets = _configurationStore == null
            ? Array.Empty<SecretDescriptor>()
            : await _configurationStore.LoadSecretsAsync(cancellationToken);
        var selectedTestId = SelectedTest?.Id;
        var selectedPageId = SelectedPage?.Id;

        foreach (var test in Tests)
        {
            Unsubscribe(test);
        }

        foreach (var page in Pages)
        {
            Unsubscribe(page);
        }

        _pageModels = snapshot.Pages.ToArray();
        UnsubscribeConfigurationEditors();
        Project = new ProjectEditorViewModel(snapshot.Project);
        Project.DirtyChanged += OnConfigurationDirtyChanged;
        Settings = new SettingsEditorViewModel(settings);
        Settings.DirtyChanged += OnConfigurationDirtyChanged;

        Secrets.Clear();
        foreach (var secret in secrets.OrderBy(secret => secret.Key, StringComparer.OrdinalIgnoreCase))
        {
            Secrets.Add(new SecretEntryViewModel(secret));
        }

        SelectedSecret = Secrets.FirstOrDefault();
        Pages.Clear();
        foreach (var page in _pageModels)
        {
            Pages.Add(CreatePageEditor(page));
        }

        Tests.Clear();
        foreach (var test in snapshot.Tests)
        {
            Tests.Add(CreateTestEditor(test));
        }

        // Restores whichever page/test was selected before this (re)load - deliberately does NOT
        // fall back to an arbitrary Tests[0]/Pages[0] default when there was nothing to restore
        // (e.g. the very first load of a workspace). An arbitrary default here reads, to every
        // command that trusts SelectedTest/SelectedPage as "the user's intent" (Run Selected Test,
        // Generate Selected), as if the user had deliberately picked that item - even though they
        // never touched it. Leaving both null instead surfaces the Workbench's own empty-state
        // prompts ("Select a test or create a new one.") rather than guessing.
        var previousPage = Pages.FirstOrDefault(page => page.Id == selectedPageId);
        var previousTest = Tests.FirstOrDefault(test => test.Id == selectedTestId);
        if (previousPage != null)
        {
            SelectedPage = previousPage;
        }
        else if (previousTest != null)
        {
            SelectedTest = previousTest;
        }

        RaiseCollectionState();
    }

    private TestEditorViewModel CreateTestEditor(TestCase test)
    {
        var editor = new TestEditorViewModel(test, () => _pageModels);
        editor.DirtyChanged += OnTestDirtyChanged;
        return editor;
    }

    private void Unsubscribe(TestEditorViewModel editor)
    {
        editor.DirtyChanged -= OnTestDirtyChanged;
    }

    private void OnTestDirtyChanged(object? sender, EventArgs e)
    {
        RaiseDirtyState();
        if (ReferenceEquals(sender, SelectedTest))
        {
            OnPropertyChanged(nameof(CanSaveSelected));
        }
    }

    private PageEditorViewModel CreatePageEditor(PageDefinition page)
    {
        var editor = new PageEditorViewModel(page);
        editor.DirtyChanged += OnPageDirtyChanged;
        return editor;
    }

    private void Unsubscribe(PageEditorViewModel editor)
    {
        editor.DirtyChanged -= OnPageDirtyChanged;
    }

    private void OnPageDirtyChanged(object? sender, EventArgs e)
    {
        RaiseDirtyState();
        if (ReferenceEquals(sender, SelectedPage))
        {
            OnPropertyChanged(nameof(CanSaveSelected));
        }
    }

    private void OnConfigurationDirtyChanged(object? sender, EventArgs e)
    {
        RaiseDirtyState();
    }

    private void UnsubscribeConfigurationEditors()
    {
        if (Project != null)
        {
            Project.DirtyChanged -= OnConfigurationDirtyChanged;
        }

        if (Settings != null)
        {
            Settings.DirtyChanged -= OnConfigurationDirtyChanged;
        }
    }

    private void ReplaceSecret(SecretDescriptor descriptor)
    {
        var existing = Secrets.FirstOrDefault(secret =>
            string.Equals(secret.Key, descriptor.Key, StringComparison.OrdinalIgnoreCase));
        var index = existing == null ? -1 : Secrets.IndexOf(existing);
        if (existing != null)
        {
            Secrets.Remove(existing);
        }

        var replacement = new SecretEntryViewModel(descriptor);
        if (index >= 0)
        {
            Secrets.Insert(index, replacement);
        }
        else
        {
            Secrets.Add(replacement);
        }

        SelectedSecret = replacement;
    }

    private static CliqSettings CreateDefaultSettings()
    {
        return new CliqSettings(
            CliqSettings.CurrentSchemaVersion,
            new ElementIdentificationSettings(
                "data-testid",
                new[] { "testId", "id", "name" },
                Array.Empty<string>()),
            new TestExecutionSettings(10000, 30000, true),
            new ScanningSettings(30000, 1000, false));
    }

    private void RaiseCollectionState()
    {
        OnPropertyChanged(nameof(FilteredTests));
        OnPropertyChanged(nameof(FilteredPages));
        RaiseDirtyState();
    }

    private void RaiseDirtyState()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(DirtySummary));
        OnPropertyChanged(nameof(CanSaveAll));
    }

    private bool MatchesSearch(string value)
    {
        return string.IsNullOrWhiteSpace(SearchText) ||
               value.IndexOf(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task<bool> RunExclusiveAsync(Func<Task<bool>> action)
    {
        await _operationLock.WaitAsync();
        try
        {
            return await AsyncOperation.RunAsync<bool>(
                busy =>
                {
                    IsBusy = busy;
                    OnPropertyChanged(nameof(IsNotBusy));
                    OnPropertyChanged(nameof(CanSaveSelected));
                    OnPropertyChanged(nameof(CanSaveAll));
                },
                s => StatusMessage = s,
                e => ErrorMessage = e,
                action);
        }
        finally
        {
            _operationLock.Release();
        }
    }
}
