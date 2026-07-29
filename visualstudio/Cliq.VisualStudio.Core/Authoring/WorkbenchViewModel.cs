using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class WorkbenchViewModel : BindableBase
{
    private readonly IWorkbenchStore _store;
    private readonly IWorkbenchPrompt _prompt;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private string _searchText = string.Empty;
    private TestEditorViewModel? _selectedTest;
    private PageInspectorViewModel? _selectedPage;
    private bool _isBusy;
    private string? _statusMessage;
    private string? _errorMessage;
    private IReadOnlyList<PageDefinition> _pageModels = Array.Empty<PageDefinition>();

    public WorkbenchViewModel(
        IWorkbenchStore store,
        IWorkbenchPrompt? prompt = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _prompt = prompt ?? new PermissiveWorkbenchPrompt();
    }

    public ObservableCollection<TestEditorViewModel> Tests { get; } = new();

    public ObservableCollection<PageInspectorViewModel> Pages { get; } = new();

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

    public bool IsDirty => Tests.Any(test => test.IsDirty);

    public string DirtySummary
    {
        get
        {
            var count = Tests.Count(test => test.IsDirty);
            return count == 0 ? "All changes saved" : $"{count} unsaved test{(count == 1 ? string.Empty : "s")}";
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
            .ToArray();
        if (errors.Length > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        return await RunExclusiveAsync(
            async () =>
            {
                foreach (var test in dirty)
                {
                    var saved = await _store.SaveTestAsync(test.ToModel(), cancellationToken);
                    test.AcceptChanges(saved);
                }

                StatusMessage = dirty.Length == 0
                    ? "No changes to save."
                    : $"Saved {dirty.Length} test{(dirty.Length == 1 ? string.Empty : "s")}.";
                RaiseDirtyState();
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
        var selectedTestId = SelectedTest?.Id;
        var selectedPageId = SelectedPage?.Id;

        foreach (var test in Tests)
        {
            Unsubscribe(test);
        }

        _pageModels = snapshot.Pages.ToArray();
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
