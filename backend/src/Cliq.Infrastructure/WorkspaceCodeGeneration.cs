using System.Text;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using DomainLocator = Cliq.Domain.LocatorStrategy;
using DomainBrowser = Cliq.Domain.BrowserKind;
using GenerationKind = Cliq.Infrastructure.Generation.ElementActionKind;
using GenerationLocator = Cliq.Infrastructure.Generation.LocatorStrategy;
using WorkspaceGenerationResult = Cliq.Domain.GenerationResult;

namespace Cliq.Infrastructure;

public sealed class WorkspaceCodeGeneration(WorkspaceRepository repository) : ICodeGeneration
{
    public async Task<WorkspaceGenerationResult> GenerateAsync(
        ProjectManifest project,
        IReadOnlyList<PageDefinition> pages,
        IReadOnlyList<TestCase> tests,
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                Path.GetFullPath(workspaceRoot),
                Path.GetFullPath(repository.RootPath ?? throw new WorkspaceNotOpenException()),
                StringComparison.Ordinal))
        {
            throw new WorkspaceValidationException("Generation workspace does not match the open workspace.");
        }

        var pageObjectDirectory = repository.GetContainedPath(project.Output.PageObjectsDirectory);
        var testDirectory = repository.GetContainedPath(project.Output.TestsDirectory);
        Directory.CreateDirectory(pageObjectDirectory);
        Directory.CreateDirectory(testDirectory);

        var settings = await repository.GetSettingsAsync(cancellationToken);

        var artifacts = new List<GeneratedArtifact>();
        var pageClassNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var className = EnsurePageSuffix(IdentifierNaming.ToPascalCase(
                IdentifierNaming.Sanitize(page.Name, "GeneratedPage")));
            pageClassNames[page.Id] = className;
            var descriptors = page.Elements
                .Select(element => ToDescriptor(element, settings.ElementIdentification.TestIdAttribute))
                .ToList();
            var pageCode = PageObjectCodeGenerator.Generate(
                className,
                project.Output.PageObjectsNamespace,
                descriptors,
                settings.TestExecution.DefaultStepTimeoutMs);
            var generatedPath = Path.Combine(pageObjectDirectory, $"{className}.cs");
            await WriteAtomicTextAsync(generatedPath, pageCode, cancellationToken);
            artifacts.Add(new GeneratedArtifact("pageObject", className, generatedPath, true));

            var customPath = Path.Combine(pageObjectDirectory, $"{className}.Custom.cs");
            var created = !File.Exists(customPath);
            if (created)
            {
                await WriteAtomicTextAsync(
                    customPath,
                    PageObjectCodeGenerator.GenerateCustomStub(className, project.Output.PageObjectsNamespace),
                    cancellationToken);
            }

            artifacts.Add(new GeneratedArtifact("custom", className, customPath, created));
        }

        var pagesById = pages.ToDictionary(page => page.Id, StringComparer.Ordinal);
        foreach (var test in tests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var className = IdentifierNaming.ToPascalCase(IdentifierNaming.Sanitize(test.Name, "GeneratedTest"));
            var code = GenerateWorkflowTest(
                project, test, pagesById, pageClassNames, className, settings.ElementIdentification.TestIdAttribute);
            var path = Path.Combine(testDirectory, $"{className}Tests.cs");
            await WriteAtomicTextAsync(path, code, cancellationToken);
            artifacts.Add(new GeneratedArtifact("test", $"{className}Tests", path, true));
        }

        return new WorkspaceGenerationResult(artifacts);
    }

    private static ElementDescriptor ToDescriptor(PageElement element, string testIdAttribute) =>
        new(
            element.Tag,
            element.Action switch
            {
                StepAction.Fill or StepAction.PressKey => GenerationKind.Fill,
                StepAction.Select => GenerationKind.Select,
                StepAction.AssertText or StepAction.AssertVisible => GenerationKind.Assert,
                _ => GenerationKind.Click,
            },
            element.Locator.Strategy switch
            {
                DomainLocator.Id => GenerationLocator.Id,
                DomainLocator.Name => GenerationLocator.Name,
                DomainLocator.XPath => GenerationLocator.XPath,
                DomainLocator.TestId or DomainLocator.Css => GenerationLocator.CssSelector,
                _ => GenerationLocator.CssSelector,
            },
            element.Locator.Strategy == DomainLocator.TestId
                ? $"[{testIdAttribute}=\"{CssAttributeEscape(element.Locator.Value)}\"]"
                : element.Locator.Value,
            element.Name,
            element.ExpectedText);

    private static string GenerateWorkflowTest(
        ProjectManifest project,
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> pageClassNames,
        string className,
        string testIdAttribute)
    {
        var driverType = project.DefaultBrowser == DomainBrowser.Edge ? "EdgeDriver" : "ChromeDriver";
        var driverUsing = project.DefaultBrowser == DomainBrowser.Edge
            ? "OpenQA.Selenium.Edge"
            : "OpenQA.Selenium.Chrome";
        var sb = new StringBuilder();
        sb.AppendLine("using NUnit.Framework;");
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine($"using {driverUsing};");
        sb.AppendLine($"using {project.Output.PageObjectsNamespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {project.Output.TestsNamespace};");
        sb.AppendLine();
        sb.AppendLine("[TestFixture]");
        sb.AppendLine($"public sealed class {className}Tests");
        sb.AppendLine("{");
        sb.AppendLine("    private IWebDriver _driver = null!;");
        sb.AppendLine();
        sb.AppendLine("    [SetUp]");
        sb.AppendLine($"    public void SetUp() => _driver = new {driverType}();");
        sb.AppendLine();
        sb.AppendLine("    [TearDown]");
        sb.AppendLine("    public void TearDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver.Quit();");
        sb.AppendLine("        _driver.Dispose();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Test]");
        sb.AppendLine($"    public void {className}Workflow()");
        sb.AppendLine("    {");

        foreach (var pageId in test.Steps.Where(step => step.PageId is not null)
                     .Select(step => step.PageId!)
                     .Distinct(StringComparer.Ordinal))
        {
            if (pageClassNames.TryGetValue(pageId, out var pageClassName))
            {
                sb.AppendLine($"        var {Variable(pageClassName)} = new {pageClassName}(_driver);");
            }
        }

        if (test.Steps.Any(step => step.PageId is not null))
        {
            sb.AppendLine();
        }

        foreach (var step in test.Steps.Where(step => step.Enabled))
        {
            AppendStep(sb, step, pages, pageClassNames, testIdAttribute);
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static string RequiredSecret(string key) =>");
        sb.AppendLine("        Environment.GetEnvironmentVariable($\"CLIQ_{key}\")");
        sb.AppendLine("        ?? throw new InvalidOperationException($\"Required environment variable CLIQ_{key} is missing.\");");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendStep(
        StringBuilder sb,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> pageClassNames,
        string testIdAttribute)
    {
        if (step.Action == StepAction.Navigate)
        {
            sb.AppendLine($"        _driver.Navigate().GoToUrl({Literal(step.Url ?? "")});");
            return;
        }

        if (step.Action == StepAction.AssertUrl)
        {
            sb.AppendLine($"        Assert.That(_driver.Url, Does.Match({Literal(step.Value?.Value ?? "")}));");
            return;
        }

        if (step.PageId is null
            || step.ElementId is null
            || !pages.TryGetValue(step.PageId, out var page)
            || !pageClassNames.TryGetValue(step.PageId, out var pageClassName))
        {
            sb.AppendLine($"        Assert.Fail({Literal($"Step '{step.Label}' has no valid page/element reference.")});");
            return;
        }

        var element = page.Elements.SingleOrDefault(item => item.Id == step.ElementId);
        if (element is null)
        {
            sb.AppendLine($"        Assert.Fail({Literal($"Element '{step.ElementId}' was not found.")});");
            return;
        }

        var member = IdentifierNaming.ToPascalCase(element.Name);
        var pageVariable = Variable(pageClassName);
        var value = ValueExpression(step.Value);
        switch (step.Action)
        {
            case StepAction.Click:
                sb.AppendLine($"        {pageVariable}.Click{member}();");
                break;
            case StepAction.Fill:
            case StepAction.PressKey:
                sb.AppendLine($"        {pageVariable}.Fill{member}({value});");
                break;
            case StepAction.Select:
                sb.AppendLine($"        {pageVariable}.Select{member}({value});");
                break;
            case StepAction.AssertText:
                sb.AppendLine($"        Assert.That({pageVariable}.Get{member}Text(), Is.EqualTo({value}));");
                break;
            case StepAction.AssertVisible:
                sb.AppendLine($"        Assert.That(_driver.FindElement({ByExpression(element, testIdAttribute)}).Displayed, Is.True);");
                break;
        }
    }

    private static string ByExpression(PageElement element, string testIdAttribute)
    {
        var locatorValue = element.Locator.Strategy == DomainLocator.TestId
            ? $"[{testIdAttribute}=\"{CssAttributeEscape(element.Locator.Value)}\"]"
            : element.Locator.Value;
        var value = Literal(locatorValue);
        return element.Locator.Strategy switch
        {
            DomainLocator.Id => $"By.Id({value})",
            DomainLocator.Name => $"By.Name({value})",
            DomainLocator.XPath => $"By.XPath({value})",
            DomainLocator.TestId => $"By.CssSelector({value})",
            _ => $"By.CssSelector({value})",
        };
    }

    private static string ValueExpression(StepValue? value) =>
        value?.Kind == "secret"
            ? $"RequiredSecret({Literal(NormalizeSecretKey(value.Value))})"
            : Literal(value?.Value ?? "");

    private static string NormalizeSecretKey(string value) =>
        string.Concat(value.Select(character => char.IsLetterOrDigit(character)
            ? char.ToUpperInvariant(character)
            : '_'));

    private static string Literal(string value) => PageObjectCodeGenerator.ToCSharpStringLiteral(value);
    private static string Variable(string className) => char.ToLowerInvariant(className[0]) + className[1..];
    private static string EnsurePageSuffix(string value) => value.EndsWith("Page", StringComparison.Ordinal) ? value : value + "Page";
    private static string CssAttributeEscape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static async Task WriteAtomicTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(temporary, contents, cancellationToken);
        File.Move(temporary, path, true);
    }
}
