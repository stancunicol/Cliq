using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cliq.Infrastructure.Generation;

/// <summary>
/// Runs a real C# parse (not just <see cref="CustomMethodExtractor"/>'s brace-walk) over an
/// extracted custom-method candidate, so the merge UI can flag one that merely happens to have
/// balanced braces but is otherwise broken (a missing semicolon, an invalid token, etc). Only
/// parses - never compiles - since a lone method has no access to the rest of the Page Object
/// class it will be re-emitted into, so semantic checks (unknown types, missing usings) would be
/// noise here.
/// </summary>
internal static class CustomMethodSyntaxChecker
{
    public static IReadOnlyList<string> FindSyntaxErrors(string methodSourceText)
    {
        var wrapped = $"class __CliqSyntaxCheck {{ {methodSourceText} }}";
        var tree = CSharpSyntaxTree.ParseText(wrapped);
        return tree.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.GetMessage())
            .ToList();
    }
}
