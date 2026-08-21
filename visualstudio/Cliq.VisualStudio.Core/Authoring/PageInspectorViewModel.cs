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
    private string _elementFilter = string.Empty;

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

    public string ElementFilter
    {
        get => _elementFilter;
        set
        {
            if (SetProperty(ref _elementFilter, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(FilteredElements));
            }
        }
    }

    public IEnumerable<PageElementInspectorViewModel> FilteredElements =>
        Elements.Where(MatchesFilter);

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
        OnPropertyChanged(nameof(FilteredElements));
        IsDirty = false;
    }

    private bool MatchesFilter(PageElementInspectorViewModel element)
    {
        if (string.IsNullOrWhiteSpace(ElementFilter))
        {
            return true;
        }

        var filter = ElementFilter.Trim();
        return element.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
               element.Tag.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
               element.Locator.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
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
        OnPropertyChanged(nameof(FilteredElements));
    }
}

public sealed class PageElementInspectorViewModel
{
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
}
