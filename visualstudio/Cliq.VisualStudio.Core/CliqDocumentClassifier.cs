using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Cliq.VisualStudio.Core;

public enum CliqDocumentKind
{
    Other,
    Test,
    Page
}

public static class CliqDocumentClassifier
{
    public static CliqDocumentKind Classify(string? path)
    {
        if (path is null ||
            path.Trim().Length == 0 ||
            !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return CliqDocumentKind.Other;
        }

        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if (ContainsCliqDirectory(normalized, "tests"))
        {
            return CliqDocumentKind.Test;
        }

        return ContainsCliqDirectory(normalized, "pages")
            ? CliqDocumentKind.Page
            : CliqDocumentKind.Other;
    }

    public static bool TryParseHttpSelection(string? selection, out Uri? uri)
    {
        var candidate = selection?.Trim();
        if (Uri.TryCreate(candidate, UriKind.Absolute, out uri) &&
            uri is not null &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        uri = null;
        return false;
    }

    private static readonly Regex LocatorFieldNamePattern = new(@"^[A-Za-z_][A-Za-z0-9_]*Locator$", RegexOptions.Compiled);

    /// <summary>
    /// Resolves the <c>// Locator (...): ...</c> comment for a generated locator field when the
    /// user has double-clicked (or otherwise selected) the field's name itself - e.g.
    /// <c>SubmitLocator</c> - rather than the comment text. The generator always emits that
    /// comment on the line directly above the field it documents (<c>PageObjectCodeGenerator.AppendStaticLocatorField</c>),
    /// so recovering it is just: confirm the selection looks like a <c>{Name}Locator</c> field
    /// name, then confirm the line above actually is a locator comment. Returns null if either
    /// check fails - used both to decide whether to show the "Locate in Browser" context-menu
    /// item and, on execute, to get the actual comment text to send to the backend (the
    /// authoritative parse of that text happens server-side in
    /// <c>SeleniumTestRunner.ResolveElementFromComment</c>).
    /// </summary>
    public static string? ResolveLocatorFieldComment(string? selectedText, string? lineAboveSelection)
    {
        var fieldName = selectedText?.Trim();
        if (string.IsNullOrEmpty(fieldName) || !LocatorFieldNamePattern.IsMatch(fieldName))
        {
            return null;
        }

        return lineAboveSelection?.TrimStart().StartsWith("// Locator", StringComparison.Ordinal) == true
            ? lineAboveSelection
            : null;
    }

    private static bool ContainsCliqDirectory(string path, string childDirectory)
    {
        var marker = string.Concat(
            Path.DirectorySeparatorChar,
            ".cliq",
            Path.DirectorySeparatorChar,
            childDirectory,
            Path.DirectorySeparatorChar);

        return path.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
