using System;
using System.IO;

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
