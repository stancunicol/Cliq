using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class TestEditorViewModel : BindableBase
{
    private readonly Func<IReadOnlyList<PageDefinition>> _pages;
    private string _name;
    private string _description;
    private string _tagsText;
    private bool _isDirty;
    private bool _isNew;
    private TestStepEditorViewModel? _selectedStep;

    public TestEditorViewModel(
        TestCase test,
        Func<IReadOnlyList<PageDefinition>> pages)
    {
        _pages = pages ?? throw new ArgumentNullException(nameof(pages));
        Id = test.Id;
        _name = test.Name;
        _description = test.Description ?? string.Empty;
        _tagsText = string.Join(", ", test.Tags);
        Steps = new ObservableCollection<TestStepEditorViewModel>(
            test.Steps.Select(step => CreateStep(step)));
        Steps.CollectionChanged += OnStepsChanged;
        _selectedStep = Steps.FirstOrDefault();
    }

    public event EventHandler? DirtyChanged;

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value ?? string.Empty))
            {
                MarkDirty();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(ValidationSummary));
            }
        }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Untitled test" : Name;

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value ?? string.Empty))
            {
                MarkDirty();
            }
        }
    }

    public string TagsText
    {
        get => _tagsText;
        set
        {
            if (SetProperty(ref _tagsText, value ?? string.Empty))
            {
                MarkDirty();
            }
        }
    }

    public ObservableCollection<TestStepEditorViewModel> Steps { get; }

    public TestStepEditorViewModel? SelectedStep
    {
        get => _selectedStep;
        set => SetProperty(ref _selectedStep, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                DirtyChanged?.Invoke(this, EventArgs.Empty);
                OnPropertyChanged(nameof(DirtyMarker));
            }
        }
    }

    public string DirtyMarker => IsDirty ? "Modified" : "Saved";

    public bool IsNew => _isNew;

    public string ValidationSummary => string.Join(
        Environment.NewLine,
        Validate());

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Test name is required.");
        }

        var duplicateTags = ParseTags()
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTags != null)
        {
            errors.Add($"Tag '{duplicateTags.Key}' is duplicated.");
        }

        for (var index = 0; index < Steps.Count; index++)
        {
            errors.AddRange(Steps[index].Validate().Select(error => $"Step {index + 1}: {error}"));
        }

        return errors;
    }

    public void AddStep()
    {
        var step = new TestStep(
            Guid.NewGuid().ToString("N"),
            StepAction.Navigate,
            "Navigate",
            true,
            Url: _pages().FirstOrDefault()?.Url ?? "https://");
        var editor = CreateStep(step);
        Steps.Add(editor);
        SelectedStep = editor;
    }

    public void DuplicateSelectedStep()
    {
        if (SelectedStep == null)
        {
            return;
        }

        var index = Steps.IndexOf(SelectedStep);
        var duplicate = SelectedStep.Duplicate();
        Subscribe(duplicate);
        Steps.Insert(index + 1, duplicate);
        SelectedStep = duplicate;
    }

    public void DeleteSelectedStep()
    {
        if (SelectedStep == null)
        {
            return;
        }

        var index = Steps.IndexOf(SelectedStep);
        var removed = SelectedStep;
        Steps.Remove(removed);
        removed.Changed -= OnStepChanged;
        SelectedStep = Steps.Count == 0 ? null : Steps[Math.Min(index, Steps.Count - 1)];
    }

    public void MoveSelectedStep(int offset)
    {
        if (SelectedStep == null)
        {
            return;
        }

        var oldIndex = Steps.IndexOf(SelectedStep);
        var newIndex = oldIndex + offset;
        if (newIndex < 0 || newIndex >= Steps.Count)
        {
            return;
        }

        Steps.Move(oldIndex, newIndex);
    }

    public TestCase ToModel()
    {
        return new TestCase(
            TestCase.CurrentSchemaVersion,
            Id,
            Name.Trim(),
            string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            ParseTags(),
            Steps.Select(step => step.ToModel()).ToArray(),
            DateTimeOffset.UtcNow);
    }

    public void AcceptChanges(TestCase saved)
    {
        _name = saved.Name;
        _description = saved.Description ?? string.Empty;
        _tagsText = string.Join(", ", saved.Tags);
        ReplaceSteps(saved.Steps);
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(TagsText));
        OnPropertyChanged(nameof(ValidationSummary));
        _isNew = false;
        OnPropertyChanged(nameof(IsNew));
        IsDirty = false;
    }

    public void NotifyPagesChanged()
    {
        foreach (var step in Steps)
        {
            step.NotifyPagesChanged();
        }

        OnPropertyChanged(nameof(ValidationSummary));
    }

    internal void MarkAsNew()
    {
        _isNew = true;
        OnPropertyChanged(nameof(IsNew));
        IsDirty = true;
    }

    private IReadOnlyList<string> ParseTags()
    {
        return TagsText
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .ToArray();
    }

    private TestStepEditorViewModel CreateStep(TestStep step)
    {
        var editor = new TestStepEditorViewModel(step, _pages);
        Subscribe(editor);
        return editor;
    }

    private void Subscribe(TestStepEditorViewModel step)
    {
        step.Changed += OnStepChanged;
    }

    private void OnStepChanged(object? sender, EventArgs e)
    {
        MarkDirty();
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private void OnStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MarkDirty();
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private void ReplaceSteps(IEnumerable<TestStep> steps)
    {
        Steps.CollectionChanged -= OnStepsChanged;
        foreach (var current in Steps)
        {
            current.Changed -= OnStepChanged;
        }

        Steps.Clear();
        foreach (var step in steps)
        {
            Steps.Add(CreateStep(step));
        }

        Steps.CollectionChanged += OnStepsChanged;
        SelectedStep = Steps.FirstOrDefault();
    }

    private void MarkDirty()
    {
        IsDirty = true;
    }
}
