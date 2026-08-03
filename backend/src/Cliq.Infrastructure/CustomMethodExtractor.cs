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
internal static class CustomMethodExtractor
{
    private const string Marker = "[" + PageObjectCodeGenerator.CustomMethodAttributeName + "]";

    public static IReadOnlyList<string> Extract(string source)
    {
        var blocks = new List<string>();
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
                // Malformed (e.g. attribute with no method after it, or an unclosed brace) -
                // skip past this occurrence rather than emit a truncated, non-compiling block.
                searchFrom = markerIndex + Marker.Length;
                continue;
            }

            blocks.Add(source[blockStart..(end + 1)].TrimEnd('\r', '\n', ' '));
            searchFrom = end + 1;
        }

        return blocks;
    }

    private static int LineStart(string source, int index)
    {
        var newline = source.LastIndexOf('\n', Math.Max(0, index - 1));
        return newline + 1;
    }

    // Re-walks from the start of the file to decide whether `position` falls inside a comment or
    // string literal that opened before it - the same skip rules FindMethodEnd/SkipBlock use, run
    // up front instead of mid-scan. O(n) per marker occurrence, which is fine at Page Object file
    // sizes (dozens of methods, not thousands).
    private static bool IsInsideCommentOrString(string source, int position)
    {
        var i = 0;
        while (i < position)
        {
            var c = source[i];

            if (c == '/' && Peek(source, i + 1) == '/')
            {
                var newline = source.IndexOf('\n', i);
                var commentEnd = newline < 0 ? source.Length : newline;
                if (position <= commentEnd)
                {
                    return true;
                }

                i = commentEnd;
                continue;
            }

            if (c == '/' && Peek(source, i + 1) == '*')
            {
                var close = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                var commentEnd = close < 0 ? source.Length : close + 2;
                if (position < commentEnd)
                {
                    return true;
                }

                i = commentEnd;
                continue;
            }

            if (c is '"' or '\'')
            {
                var literalEnd = SkipLiteral(source, i);
                if (position < literalEnd)
                {
                    return true;
                }

                i = literalEnd;
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
            var c = source[i];

            if (c == '/' && Peek(source, i + 1) == '/')
            {
                var newline = source.IndexOf('\n', i);
                if (newline < 0)
                {
                    return -1;
                }

                i = newline;
                continue;
            }

            if (c == '/' && Peek(source, i + 1) == '*')
            {
                var close = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (close < 0)
                {
                    return -1;
                }

                i = close + 2;
                continue;
            }

            if (c is '"' or '\'')
            {
                i = SkipLiteral(source, i);
                continue;
            }

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
            var c = source[i];

            if (c == '/' && Peek(source, i + 1) == '/')
            {
                var newline = source.IndexOf('\n', i);
                if (newline < 0)
                {
                    return -1;
                }

                i = newline;
                continue;
            }

            if (c == '/' && Peek(source, i + 1) == '*')
            {
                var close = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (close < 0)
                {
                    return -1;
                }

                i = close + 2;
                continue;
            }

            if (c is '"' or '\'')
            {
                i = SkipLiteral(source, i);
                continue;
            }

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
