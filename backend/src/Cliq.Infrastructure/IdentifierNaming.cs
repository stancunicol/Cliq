using System.Text.RegularExpressions;

namespace Cliq.Infrastructure.Generation;

public static class IdentifierNaming
{
    private static readonly HashSet<string> ReservedWords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
        "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
        "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    public static string Sanitize(string? raw, string fallback)
    {
        var value = Regex.Replace(raw ?? "", "[^0-9a-zA-Z]+", "_").Trim('_').ToLowerInvariant();
        value = Regex.Replace(value, "^[0-9_]+", "");
        if (string.IsNullOrEmpty(value) || ReservedWords.Contains(value))
        {
            value = fallback;
        }

        return value;
    }

    public static string ToPascalCase(string snakeCaseName)
    {
        var parts = snakeCaseName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static readonly Regex SafeIdentifierPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static bool IsSafeIdentifier(string? value) =>
        !string.IsNullOrEmpty(value) && SafeIdentifierPattern.IsMatch(value);

    /// <summary>Same guarantee as <see cref="IsSafeIdentifier"/>, but allows dot-separated segments (a C# namespace).</summary>
    public static bool IsSafeNamespace(string? value) =>
        !string.IsNullOrEmpty(value) && value.Split('.').All(IsSafeIdentifier);

    private static readonly Regex SafeScreenshotFileNamePattern = new(@"^[A-Za-z0-9_]+\.png$", RegexOptions.Compiled);

    public static bool IsSafeFileName(string? value) =>
        !string.IsNullOrEmpty(value) && SafeScreenshotFileNamePattern.IsMatch(value);
}
