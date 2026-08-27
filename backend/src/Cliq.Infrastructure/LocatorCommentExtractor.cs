using System.Text.RegularExpressions;

namespace Cliq.Infrastructure;

/// <summary>
/// A single <c>// Locator (...)</c> comment parsed out of a generated Page Object file - the
/// label (css/xpath/name/...), the generated locator name when present (the editable identifier the
/// generator writes as <c>Name = value</c>), the locator value, and whether it documents a dynamic
/// (parameterized) element template.
/// </summary>
internal readonly record struct LocatorComment(
    string Label,
    string? Name,
    string Value,
    bool IsTemplate);

/// <summary>
/// Reads every <c>// Locator (...)</c> comment out of a previously generated Page Object file so
/// <see cref="WorkspaceCodeGeneration"/> can re-apply a user's hand-edit of the locator's name in
/// the comment on the next regeneration. Mirrors the format written by
/// <see cref="Generation.PageObjectCodeGenerator"/> and the single-comment parse in
/// <see cref="SeleniumTestRunner.Locate"/> - the name is optional so older name-less comments still
/// read correctly (and simply contribute no rename).
/// </summary>
internal static class LocatorCommentExtractor
{
    private static readonly Regex CommentPattern = new(
        @"^\s*//\s*Locator\s*\((?<label>\w+)(?:,\s*(?<template>template))?\)\s*:\s*(?:(?<name>\w+)\s*=\s*)?(?<value>.+?)\s*$",
        RegexOptions.Compiled);

    public static IReadOnlyList<LocatorComment> Extract(string source)
    {
        var comments = new List<LocatorComment>();
        foreach (var line in source.Split('\n'))
        {
            var match = CommentPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            comments.Add(new LocatorComment(
                match.Groups["label"].Value,
                match.Groups["name"].Success ? match.Groups["name"].Value : null,
                match.Groups["value"].Value,
                match.Groups["template"].Success));
        }

        return comments;
    }
}
