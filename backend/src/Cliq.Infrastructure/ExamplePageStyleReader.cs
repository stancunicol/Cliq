using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cliq.Infrastructure.Generation;

/// <summary>
/// Naming style pulled from an existing page object the user points Cliq at
/// (<see cref="Cliq.Domain.CliqSettings.ExamplePageObjectFilePath"/>) - not a base class to
/// inherit from, just a style hint for the shared <c>BasePage</c> Cliq itself generates (see
/// <see cref="PageObjectCodeGenerator.GenerateBasePageFile"/>). Every field always has a value -
/// there's nothing to fail against here, unlike the earlier "read a foreign base class" design
/// this replaces, since Cliq now authors the base class itself and falls back to its own
/// defaults when the example doesn't offer a clearer signal.
/// </summary>
public sealed record ExamplePageStyle(
    string DriverParameterName,
    string DriverFieldName,
    // The example's own WebDriverWait-typed member name, if it has one - null means the example
    // doesn't declare one (common; see the real PageFactory-style fixture this was built
    // against), in which case BasePageFieldNames.FromStyle derives a name from DriverFieldName's
    // prefix instead. An explicit name here always wins over that derivation.
    string? WaitFieldName = null)
{
    public static readonly ExamplePageStyle Default = new("driver", "_driver");
}

/// <summary>
/// Reads an example page object with Roslyn (syntax-only, same spirit as
/// <see cref="CustomMethodSyntaxChecker"/>) to extract <see cref="ExamplePageStyle"/>.
/// Deliberately narrow: only naming (driver parameter/field) is mirrored - see
/// <see cref="PageObjectCodeGenerator.GenerateBasePageFile"/>'s remarks for why structural
/// choices (e.g. whether to wait for elements) are never taken from the example, only styled.
/// </summary>
internal static class ExamplePageStyleReader
{
    public static ExamplePageStyle Read(string exampleSourceText)
    {
        var root = CSharpSyntaxTree.ParseText(exampleSourceText).GetRoot();
        var classDeclaration = FindPageClass(root);
        if (classDeclaration is null)
        {
            return ExamplePageStyle.Default;
        }

        var driverParameterName = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .FirstOrDefault(parameter => IsTypeNamed(parameter.Type, "IWebDriver"))
            ?.Identifier.Text
            ?? ExamplePageStyle.Default.DriverParameterName;

        var driverFieldName = FindMemberName(classDeclaration, "IWebDriver") ?? ExamplePageStyle.Default.DriverFieldName;
        var waitFieldName = FindMemberName(classDeclaration, "WebDriverWait");

        return new ExamplePageStyle(driverParameterName, driverFieldName, waitFieldName);
    }

    /// <summary>
    /// Picks which class in the example file to read style from - skips nested classes (a private
    /// helper type declared inside the real page class would otherwise shadow it if it happened to
    /// come first in a document-order scan) and, among the remaining top-level candidates, prefers
    /// one with a constructor taking an <c>IWebDriver</c> parameter - the strongest available
    /// signal that it's actually the page object, not some unrelated top-level class that happens
    /// to appear earlier in the file. Falls back to the first top-level class when no candidate has
    /// that signal (e.g. the file has exactly one class and it has no such constructor yet).
    /// </summary>
    private static ClassDeclarationSyntax? FindPageClass(SyntaxNode root)
    {
        var topLevelClasses = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(candidate => candidate.Parent is not ClassDeclarationSyntax)
            .ToList();

        return topLevelClasses.FirstOrDefault(HasWebDriverConstructorParameter) ?? topLevelClasses.FirstOrDefault();
    }

    private static bool HasWebDriverConstructorParameter(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Any(parameter => IsTypeNamed(parameter.Type, "IWebDriver"));

    /// <summary>
    /// Strips <paramref name="driverFieldName"/>'s naming prefix (<c>_</c>, <c>m_</c>, or none)
    /// and rebuilds the same prefix in front of "wait" - one heuristic derived from the driver
    /// field's own convention, rather than a second, independent detection that could disagree
    /// with it (e.g. the example naming its driver field `driver` but its (hypothetical) wait
    /// field `_wait` - inconsistent within the same file, not a real convention to mirror).
    /// </summary>
    public static string DeriveWaitFieldName(string driverFieldName)
    {
        if (driverFieldName.StartsWith("m_", StringComparison.Ordinal))
        {
            return "m_wait";
        }

        return driverFieldName.StartsWith('_') ? "_wait" : "wait";
    }

    /// <summary>Finds the first field or property in the class declared with type
    /// <paramref name="simpleName"/> and returns its name - used for both the driver
    /// (<c>IWebDriver</c>, always present in the result, falling back to Cliq's default) and the
    /// wait field (<c>WebDriverWait</c>, left null when absent - see <see cref="ExamplePageStyle.WaitFieldName"/>).</summary>
    private static string? FindMemberName(ClassDeclarationSyntax classDeclaration, string simpleName)
    {
        foreach (var member in classDeclaration.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field when IsTypeNamed(field.Declaration.Type, simpleName):
                    return field.Declaration.Variables.FirstOrDefault()?.Identifier.Text;
                case PropertyDeclarationSyntax property when IsTypeNamed(property.Type, simpleName):
                    return property.Identifier.Text;
            }
        }

        return null;
    }

    // Textual match only (last '.'-segment) - no semantic model of the example's own project, so
    // a fully-qualified `OpenQA.Selenium.IWebDriver` and a bare `IWebDriver` both read as the
    // same name here. Same accepted gap as the design this replaces.
    private static bool IsTypeNamed(TypeSyntax? type, string simpleName)
    {
        if (type is null)
        {
            return false;
        }

        var text = type.ToString();
        var lastDot = text.LastIndexOf('.');
        var last = lastDot < 0 ? text : text[(lastDot + 1)..];
        return last == simpleName;
    }
}
