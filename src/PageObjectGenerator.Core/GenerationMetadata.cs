namespace PageObjectGenerator.Core;

/// <summary>
/// Everything needed to reopen a previously generated page and re-check its
/// elements, without re-parsing the generated C# source. Persisted as a
/// sidecar JSON file next to the generated .cs files.
/// </summary>
public sealed record GenerationMetadata(
    string ClassName,
    string Url,
    BrowserKind Browser,
    string Namespace,
    IReadOnlyList<ElementDescriptor> Elements);
