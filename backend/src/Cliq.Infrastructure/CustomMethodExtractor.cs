using System.Text;

namespace Cliq.Infrastructure.Generation;

/// <summary>
/// Pulls hand-written methods marked with <c>[CliqCustomMethod]</c> out of a previously generated
/// Page Object file so <see cref="PageObjectCodeGenerator.Generate"/> can re-emit them verbatim
/// into the freshly generated file, instead of losing them. Text-based rather than a full C#
/// parse: a single forward scan that stays aware of string/char literals and comments so a brace
/// inside one of those can't desynchronize the depth count. It does not understand C# 11 raw
/// string literals ("""...""") - a custom method using one won't be extracted correctly.
/// </summary>
internal readonly record struct ExtractedSpan(string SourceText, bool ClosedCleanly);

internal static class CustomMethodExtractor
{
    private const string Marker = "[" + PageObjectCodeGenerator.CustomMethodAttributeName + "]";

    /// <summary>
    /// Independent, deliberately naive count of how many times the marker text appears in
    /// <paramref name="source"/> - a plain substring scan with no comment/string/brace awareness
    /// at all. Used as a cross-check against <see cref="Extract"/>: since it shares no code path
    /// with the brace-walking scanner, it can't miss a method for the same reason that scanner
    /// might (e.g. a raw string literal desynchronizing the depth count). A lower
    /// <see cref="Extract"/> result than this count means something didn't come out cleanly, even
    /// without knowing why.
    /// </summary>
    public static int CountMarkerOccurrences(string source)
    {
        var count = 0;
        var searchFrom = 0;
        while (true)
        {
            var index = source.IndexOf(Marker, searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            searchFrom = index + Marker.Length;
        }
    }

    /// <summary>
    /// Whether every <c>{</c> in <paramref name="source"/> is matched by a <c>}</c> (comments and
    /// string/char literals excluded from the count, same skip rules as the rest of this class).
    /// A single brace the user deleted or added anywhere in the file - not necessarily inside a
    /// custom method's own body - can make <see cref="Extract"/>'s per-method depth-walk latch onto
    /// the wrong closing brace (most commonly the class's own) and report a clean, matching-count
    /// "recovery" for a method that isn't actually intact. Callers should treat every extracted
    /// method as untrustworthy when this returns false, regardless of what <see cref="Extract"/>'s
    /// count says, rather than silently re-emitting a misattributed span as if it were fine.
    /// </summary>
    public static bool IsBalanced(string source)
    {
        var depth = 0;
        var i = 0;
        while (i < source.Length)
        {
            if (TrySkipCommentOrLiteral(source, i, out _, out var next))
            {
                // An unterminated comment/literal advances `next` to source.Length, which ends
                // the loop on the next condition check exactly as a `break` would have - so no
                // special-casing is needed here, unlike FindMethodEnd/SkipBlock which must
                // distinguish "ran out of source" from "found the end" (see their own comments).
                i = next;
                continue;
            }

            var c = source[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth < 0)
                {
                    return false;
                }
            }

            i++;
        }

        return depth == 0;
    }

    public static IReadOnlyList<string> Extract(string source) =>
        ExtractAll(source)
            .Where(span => span.ClosedCleanly)
            .Select(span => span.SourceText)
            .ToList();

    /// <summary>
    /// Like <see cref="Extract"/> but also surfaces markers whose method body couldn't be closed
    /// cleanly, instead of silently skipping them - the merge UI needs something concrete to show
    /// the user for a broken custom method, not just a missing count. A malformed span's
    /// <see cref="ExtractedSpan.SourceText"/> is a best-effort slice from the marker's line up to
    /// the next marker (or end of file), which may include unrelated trailing code if the actual
    /// damage runs past that point - there's no reliable way to know where a malformed block
    /// "should" have ended.
    /// </summary>
    public static IReadOnlyList<ExtractedSpan> ExtractAll(string source)
    {
        var spans = new List<ExtractedSpan>();
        var searchFrom = 0;
        while (true)
        {
            var markerIndex = source.IndexOf(Marker, searchFrom, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                break;
            }

            // A stray match inside a comment or string (e.g. someone writes "see [CliqCustomMethod]
            // docs" in a remark) is not a real attribute application - skip it rather than risk
            // extracting garbage from wherever the text happens to sit.
            if (IsInsideCommentOrString(source, markerIndex))
            {
                searchFrom = markerIndex + Marker.Length;
                continue;
            }

            var blockStart = LineStart(source, markerIndex);
            var end = FindMethodEnd(source, markerIndex + Marker.Length);
            if (end < 0)
            {
                var nextMarker = source.IndexOf(Marker, markerIndex + Marker.Length, StringComparison.Ordinal);
                var fallbackEnd = nextMarker < 0 ? source.Length : nextMarker;
                var raw = source[blockStart..fallbackEnd].TrimEnd('\r', '\n', ' ');
                spans.Add(new ExtractedSpan(raw, ClosedCleanly: false));
                searchFrom = fallbackEnd;
                continue;
            }

            spans.Add(new ExtractedSpan(
                source[blockStart..(end + 1)].TrimEnd('\r', '\n', ' '),
                ClosedCleanly: true));
            searchFrom = end + 1;
        }

        return spans;
    }

    // Walks back past the marker's own line to also capture any XML doc comment (///) or other
    // attribute ([...]) lines stacked directly above it, so a user's docstring or an extra
    // attribute above [CliqCustomMethod] isn't silently dropped on the next regeneration. Stops
    // at the first line that isn't one of those (including a blank line, treated as a deliberate
    // separator).
    private static int LineStart(string source, int index)
    {
        var start = ImmediateLineStart(source, index);
        while (start > 0)
        {
            var newlineBeforeStart = start - 1;
            var previousStart = ImmediateLineStart(source, newlineBeforeStart);
            var previousLine = source[previousStart..newlineBeforeStart].TrimEnd('\r').Trim();
            if (!IsAttributeOrDocCommentLine(previousLine))
            {
                break;
            }

            start = previousStart;
        }

        return start;
    }

    private static int ImmediateLineStart(string source, int index)
    {
        var newline = source.LastIndexOf('\n', Math.Max(0, index - 1));
        return newline + 1;
    }

    private static bool IsAttributeOrDocCommentLine(string trimmedLine) =>
        trimmedLine.StartsWith("///", StringComparison.Ordinal)
        || (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'));

    // Re-walks from the start of the file to decide whether `position` falls inside a comment or
    // string literal that opened before it - the same skip rules FindMethodEnd/SkipBlock use, run
    // up front instead of mid-scan. O(n) per marker occurrence, which is fine at Page Object file
    // sizes (dozens of methods, not thousands).
    private static bool IsInsideCommentOrString(string source, int position)
    {
        var i = 0;
        while (i < position)
        {
            if (TrySkipCommentOrLiteral(source, i, out var kind, out var end))
            {
                // A line comment's `end` is the newline index itself (not past it), so landing
                // exactly there still counts as "inside" - hence <= here but < for block comments
                // and literals, whose `end` already points one past the closing token.
                var inside = kind == CommentOrLiteralKind.LineComment ? position <= end : position < end;
                if (inside)
                {
                    return true;
                }

                i = end;
                continue;
            }

            i++;
        }

        return false;
    }

    /// <summary>
    /// Walks forward from just after the attribute, through the method signature, and returns
    /// the index of the character that ends the method: the matching '}' for a block body, or
    /// the ';' for an expression-bodied one (<c>=&gt; ...;</c>). Braces/parens inside strings,
    /// chars, and comments are skipped over so they can't be mistaken for structural ones.
    /// </summary>
    private static int FindMethodEnd(string source, int start)
    {
        var parenDepth = 0;
        var sawArrow = false;
        var i = start;
        while (i < source.Length)
        {
            // An unterminated comment/literal advances `i` to source.Length here too, same as
            // IsBalanced - the loop ends on the next condition check and falls through to the
            // `return -1` below, which is exactly what the old early-return on an unterminated
            // comment produced, just one step later.
            if (TrySkipCommentOrLiteral(source, i, out _, out var next))
            {
                i = next;
                continue;
            }

            var c = source[i];
            if (parenDepth == 0 && c == '{')
            {
                return SkipBlock(source, i);
            }

            if (c == '(')
            {
                parenDepth++;
            }
            else if (c == ')')
            {
                parenDepth--;
            }
            else if (parenDepth == 0 && c == '=' && Peek(source, i + 1) == '>')
            {
                sawArrow = true;
                i += 2;
                continue;
            }
            else if (parenDepth == 0 && sawArrow && c == ';')
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    private static int SkipBlock(string source, int openBraceIndex)
    {
        var depth = 0;
        var i = openBraceIndex;
        while (i < source.Length)
        {
            if (TrySkipCommentOrLiteral(source, i, out _, out var next))
            {
                i = next;
                continue;
            }

            var c = source[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }

            i++;
        }

        return -1;
    }

    private enum CommentOrLiteralKind
    {
        LineComment,
        BlockComment,
        Literal,
    }

    /// <summary>
    /// The single shared "skip forward one token" step every scanner in this file needs: if
    /// <paramref name="source"/>[<paramref name="index"/>] starts a <c>//</c> comment, a
    /// <c>/* */</c> comment, or a string/char literal, advances past it and returns true with
    /// <paramref name="end"/> set to the resulting index (<c>source.Length</c> if a comment runs
    /// off the end of the file without closing - <see cref="SkipLiteral"/> already does the same
    /// for an unterminated literal). Returns false, leaving <paramref name="end"/> unset, when
    /// <paramref name="index"/> is just an ordinary character the caller should handle itself.
    /// Previously this same classify-and-skip logic was reimplemented independently in four
    /// places in this class; a fix or extension to the skip rules (this class's remarks note one
    /// known gap - C# 11 raw string literals) only has to be made here now.
    /// </summary>
    private static bool TrySkipCommentOrLiteral(
        string source,
        int index,
        out CommentOrLiteralKind kind,
        out int end)
    {
        var c = source[index];
        if (c == '/' && Peek(source, index + 1) == '/')
        {
            kind = CommentOrLiteralKind.LineComment;
            var newline = source.IndexOf('\n', index);
            end = newline < 0 ? source.Length : newline;
            return true;
        }

        if (c == '/' && Peek(source, index + 1) == '*')
        {
            kind = CommentOrLiteralKind.BlockComment;
            var close = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
            end = close < 0 ? source.Length : close + 2;
            return true;
        }

        if (c is '"' or '\'')
        {
            kind = CommentOrLiteralKind.Literal;
            end = SkipLiteral(source, index);
            return true;
        }

        kind = default;
        end = index;
        return false;
    }

    // Handles regular ("...", with \" escapes), interpolated ($"...", same escapes), and
    // verbatim (@"..."/$@".../@$"..., with "" escapes) strings, plus char literals ('.', \').
    private static int SkipLiteral(string source, int quoteIndex)
    {
        if (source[quoteIndex] == '\'')
        {
            var i = quoteIndex + 1;
            while (i < source.Length)
            {
                if (source[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (source[i] == '\'')
                {
                    return i + 1;
                }

                i++;
            }

            return source.Length;
        }

        var prefixStart = Math.Max(0, quoteIndex - 2);
        var isVerbatim = source[prefixStart..quoteIndex].Contains('@');
        if (isVerbatim)
        {
            var i = quoteIndex + 1;
            while (i < source.Length)
            {
                if (source[i] == '"')
                {
                    if (Peek(source, i + 1) == '"')
                    {
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }

                i++;
            }

            return source.Length;
        }

        {
            var i = quoteIndex + 1;
            while (i < source.Length)
            {
                if (source[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (source[i] == '"')
                {
                    return i + 1;
                }

                i++;
            }

            return source.Length;
        }
    }

    private static char? Peek(string source, int index) => index < source.Length ? source[index] : null;
}
