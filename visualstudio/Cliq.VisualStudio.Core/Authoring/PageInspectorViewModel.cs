using System.Collections.Generic;
using System.Linq;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class PageInspectorViewModel
{
    public PageInspectorViewModel(PageDefinition page)
    {
        Model = page;
        Elements = page.Elements.Select(element => new PageElementInspectorViewModel(element)).ToArray();
    }

    public PageDefinition Model { get; }

    public string Id => Model.Id;

    public string Name => Model.Name;

    public string Url => Model.Url;

    public IReadOnlyList<PageElementInspectorViewModel> Elements { get; }
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
