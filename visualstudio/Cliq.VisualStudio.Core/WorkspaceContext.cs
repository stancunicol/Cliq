using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cliq.VisualStudio.Core;

public sealed class IdeContextSnapshot
{
    public IdeContextSnapshot(
        string? solutionDirectory,
        string? selectedProjectDirectory,
        string? selectedItemPath,
        string? activeDocumentPath,
        string? selectedText)
    {
        SolutionDirectory = NormalizeDirectory(solutionDirectory);
        SelectedProjectDirectory = NormalizeDirectory(selectedProjectDirectory);
        SelectedItemPath = NormalizePath(selectedItemPath);
        ActiveDocumentPath = NormalizePath(activeDocumentPath);
        SelectedText = selectedText;
    }

    public string? SolutionDirectory { get; }
    public string? SelectedProjectDirectory { get; }
    public string? SelectedItemPath { get; }
    public string? ActiveDocumentPath { get; }
    public string? SelectedText { get; }

    public CliqDocumentKind SelectedDocumentKind =>
        CliqDocumentClassifier.Classify(SelectedItemPath ?? ActiveDocumentPath);

    public string? SelectedCliqEntityId
    {
        get
        {
            var path = SelectedItemPath ?? ActiveDocumentPath;
            return SelectedDocumentKind == CliqDocumentKind.Other || path is null
                ? null
                : Path.GetFileNameWithoutExtension(path);
        }
    }

    private static string? NormalizeDirectory(string? path)
    {
        var normalized = NormalizePath(path);
        return normalized is not null && Directory.Exists(normalized) ? normalized : null;
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            return null;
        }
    }
}

public static class WorkspacePathResolver
{
    public static string? ResolveDefault(IdeContextSnapshot context, string? userOverride)
    {
        var normalizedOverride = NormalizeExistingDirectory(userOverride);
        if (normalizedOverride is not null)
        {
            return normalizedOverride;
        }

        return context.SolutionDirectory
            ?? FindWorkspaceRoot(context.SelectedItemPath)
            ?? context.SelectedProjectDirectory
            ?? FindWorkspaceRoot(context.ActiveDocumentPath);
    }

    public static string? ResolveForSelectedScope(IdeContextSnapshot context)
    {
        var cliqRoot = FindWorkspaceRoot(context.SelectedItemPath ?? context.ActiveDocumentPath);
        return cliqRoot ?? context.SelectedProjectDirectory ?? context.SolutionDirectory;
    }

    public static string? FindWorkspaceRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            return null;
        }

        var directory = Directory.Exists(fullPath)
            ? new DirectoryInfo(fullPath)
            : new FileInfo(fullPath).Directory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".cliq", "project.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static string OverrideKey(string solutionOrFolderPath)
    {
        var normalized = Path.GetFullPath(solutionOrFolderPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant();
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }

    private static string? NormalizeExistingDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            return Directory.Exists(fullPath) ? fullPath : null;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            return null;
        }
    }
}

public enum WorkspaceTransitionDecision
{
    Save,
    Discard,
    Cancel
}

public sealed class WorkspaceTransitionGuard
{
    public bool IsDirty { get; set; }

    public bool CanTransition(Func<WorkspaceTransitionDecision> prompt, Action save, Action discard)
    {
        if (!IsDirty)
        {
            return true;
        }

        switch (prompt())
        {
            case WorkspaceTransitionDecision.Save:
                save();
                IsDirty = false;
                return true;
            case WorkspaceTransitionDecision.Discard:
                discard();
                IsDirty = false;
                return true;
            default:
                return false;
        }
    }
}
