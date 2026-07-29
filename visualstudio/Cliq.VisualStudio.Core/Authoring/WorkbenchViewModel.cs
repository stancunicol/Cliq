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
    private PageInspectorViewModel? _selectedPage;
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

    public ObservableCollection<PageInspectorViewModel> Pages { get; } = new();

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
        set => SetProperty(ref _selectedSecret, value);
    }

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
        Tests.Where(test => MatchesSearch(test.Name));

    public IEnumerable<PageInspectorViewModel> FilteredPages =>
        Pages.Where(page =>
            MatchesSearch(page.Name) ||
            MatchesSearch(page.Url));

    public TestEditorViewModel? SelectedTest
    {
        get => _selectedTest;
        set => SetProperty(ref _selectedTest, value);
    }

    public PageInspectorViewModel? SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsDirty =>
        Tests.Any(test => test.IsDirty) ||
        Project?.IsDirty == true ||
        Settings?.IsDirty == true;

    public string DirtySummary
    {
        get
        {
            var count = Tests.Count(test => test.IsDirty) +
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

    public async Task<bool> SaveSelectedAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedTest;
        if (selected == null)
        {
            return false;
        }

        var errors = selected.Validate();
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                var saved = await _store.SaveTestAsync(selected.ToModel(), cancellationToken);
                selected.AcceptChanges(saved);
                StatusMessage = $"Saved {saved.Name}.";
                RaiseDirtyState();
                return true;
            });
    }

    public async Task<bool> SaveAllAsync(CancellationToken cancellationToken = default)
    {
        var dirty = Tests.Where(test => test.IsDirty).ToArray();
        var errors = dirty
            .SelectMany(test => test.Validate().Select(error => $"{test.DisplayName}: {error}"))
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
                foreach (var test in dirty)
                {
                    var saved = await _store.SaveTestAsync(test.ToModel(), cancellationToken);
                    test.AcceptChanges(saved);
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

                var savedItemCount = dirty.Length +
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

        var errors = Project.Validate();
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                var saved = await _configurationStore.SaveProjectAsync(
                    Project.ToModel(),
                    cancellationToken);
                Project.AcceptChanges(saved);
                StatusMessage = "Project settings saved.";
                RaiseDirtyState();
                return true;
            });
    }

    public async Task<bool> SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (Settings == null || _configurationStore == null)
        {
            ErrorMessage = "Cliq settings are not available until a workspace is connected.";
            return false;
        }

        var errors = Settings.Validate();
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                var saved = await _configurationStore.SaveSettingsAsync(
                    Settings.ToModel(),
                    cancellationToken);
                Settings.AcceptChanges(saved);
                StatusMessage = "Cliq settings saved.";
                RaiseDirtyState();
                return true;
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
        if (!Regex.IsMatch(normalizedKey, @"^[A-Za-z_][A-Za-z0-9_.-]*$"))
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
            Pages.Add(new PageInspectorViewModel(page));
        }

        Tests.Clear();
        foreach (var test in snapshot.Tests)
        {
            Tests.Add(CreateTestEditor(test));
        }

        SelectedTest = Tests.FirstOrDefault(test => test.Id == selectedTestId) ?? Tests.FirstOrDefault();
        SelectedPage = Pages.FirstOrDefault(page => page.Id == selectedPageId) ?? Pages.FirstOrDefault();
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
    }

    private bool MatchesSearch(string value)
    {
        return string.IsNullOrWhiteSpace(SearchText) ||
               value.IndexOf(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task<bool> RunExclusiveAsync(Func<Task<bool>> action)
    {
        await _operationLock.WaitAsync();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
            return false;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
            _operationLock.Release();
        }
    }
}
