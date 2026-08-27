using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class PageEditorViewModel : EditableViewModelBase
{
    private PageDefinition _model;
    private string _name;

    public PageEditorViewModel(PageDefinition page)
    {
        _model = page;
        _name = page.Name;
        Elements = new ObservableCollection<PageElementInspectorViewModel>(
            page.Elements.Select(element => new PageElementInspectorViewModel(element)));
    }

    public string Id => _model.Id;

    public string Name
    {
        get => _name;
        set
        {
            if (SetEditable(ref _name, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Untitled page" : Name;

    public string Url => _model.Url;

    public ObservableCollection<PageElementInspectorViewModel> Elements { get; }

    public bool HasNotFoundElements => Elements.Any(element => element.NotFound);

    public override string ValidationSummary => string.Join(Environment.NewLine, Validate());

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Page name is required.");
        }

        return errors;
    }

    public PageDefinition ToModel() =>
        _model with { Name = Name.Trim(), Elements = Elements.Select(element => element.Model).ToArray() };

    public void AcceptChanges(PageDefinition saved)
    {
        _model = saved;
        _name = saved.Name;
        Elements.Clear();
        foreach (var element in saved.Elements)
        {
            Elements.Add(new PageElementInspectorViewModel(element));
        }

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(HasNotFoundElements));
        IsDirty = false;
    }

    /// <summary>
    /// Bulk-clears every element still flagged from a stale scan (see <see cref="PageElementInspectorViewModel.NotFound"/>) -
    /// the only way to do this today is hand-editing the page's JSON file under .cliq/pages.
    /// </summary>
    public void RemoveNotFoundElements()
    {
        var stale = Elements.Where(element => element.NotFound).ToArray();
        if (stale.Length == 0)
        {
            return;
        }

        foreach (var element in stale)
        {
            Elements.Remove(element);
        }

        MarkDirty();
        OnPropertyChanged(nameof(HasNotFoundElements));
    }

    /// <summary>
    /// Reflects the live-highlight stream's current element (see
    /// <c>WorkspaceActionsViewModel.HighlightedElementId</c>) onto this page's own element
    /// list - at most one <see cref="PageElementInspectorViewModel.IsHoverHighlighted"/> is ever
    /// true at a time. Cheap enough to call on every stream tick (typically a handful of elements
    /// per page) without tracking which one was previously lit.
    /// </summary>
    public void SetHighlightedElement(string? elementId)
    {
        foreach (var element in Elements)
        {
            element.IsHoverHighlighted = elementId is not null && element.Model.Id == elementId;
        }
    }
}

public sealed class PageElementInspectorViewModel : BindableBase
{
    private bool _isHoverHighlighted;

    public PageElementInspectorViewModel(PageElement element)
    {
        Model = element;
    }

    public PageElement Model { get; }

    public string Name => Model.Name;

    public string Tag => Model.Tag;

    public StepAction Action => Model.Action;

    public string Locator => $"{Model.Locator.Strategy}: {Model.Locator.Value}";

    public string Status => Model.NotFound ? "Not found on last scan" : "Available";

    public bool NotFound => Model.NotFound;

    /// <summary>True while the live-highlight stream (see <see cref="PageEditorViewModel.SetHighlightedElement"/>)
    /// reports the mouse as hovering this element in the already-open browser session - the
    /// Page Inspector grid binds a row style trigger to this for the actual visual highlight.</summary>
    public bool IsHoverHighlighted
    {
        get => _isHoverHighlighted;
        set => SetProperty(ref _isHoverHighlighted, value);
    }
}
