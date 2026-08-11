using System.Text;
using System.Text.Json;
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
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodResolutions = null)
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
        var generationState = await LoadGenerationStateAsync(cancellationToken);

        var artifacts = new List<GeneratedArtifact>();
        var warnings = new List<CustomMethodRecoveryWarning>();
        var pendingMerges = new List<PendingCustomMethodMerge>();

        var attributePath = Path.Combine(pageObjectDirectory, "CliqCustomMethodAttribute.cs");
        await WriteAtomicTextAsync(
            attributePath,
            PageObjectCodeGenerator.GenerateAttributeFile(project.Output.PageObjectsNamespace),
            cancellationToken);
        artifacts.Add(new GeneratedArtifact("attribute", "CliqCustomMethodAttribute", attributePath, true));

        var wrapperPath = Path.Combine(pageObjectDirectory, "ElementWrapper.cs");
        await WriteAtomicTextAsync(
            wrapperPath,
            PageObjectCodeGenerator.GenerateElementWrapperFile(project.Output.PageObjectsNamespace),
            cancellationToken);
        artifacts.Add(new GeneratedArtifact("wrapper", "ElementWrapper", wrapperPath, true));

        var pageClassNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var className = IdentifierNaming.ToPageObjectClassName(page.Name);
            pageClassNames[page.Id] = className;

            var wasRenamed = generationState.TryGetValue(page.Id, out var previousClassName)
                && !string.Equals(previousClassName, className, StringComparison.Ordinal);
            var renameResolved = true;
            if (wasRenamed)
            {
                renameResolved = await HandleRenamedPageObjectAsync(
                    pageObjectDirectory,
                    page.Id,
                    previousClassName!,
                    customMethodResolutions,
                    warnings,
                    pendingMerges,
                    cancellationToken);
            }

            // Deliberately NOT advanced while a rename conflict is still unresolved, so the next
            // call re-detects it (and re-offers the old file's custom methods) instead of the
            // orphaned file silently falling out of view after the first, unresolved detection.
            if (!wasRenamed || renameResolved)
            {
                generationState[page.Id] = className;
            }

            var descriptors = page.Elements
                .Select(element => ToDescriptor(element, settings.ElementIdentification.TestIdAttribute))
                .ToList();
            var dynamicDescriptors = (page.DynamicElements ?? [])
                .Select(element => ToDynamicDescriptor(element, settings.ElementIdentification.TestIdAttribute))
                .ToList();
            var generatedPath = Path.Combine(pageObjectDirectory, $"{className}.cs");
            var existingSource = File.Exists(generatedPath)
                ? await File.ReadAllTextAsync(generatedPath, cancellationToken)
                : null;

            IReadOnlyList<string> customMethods;
            if (customMethodResolutions is not null &&
                customMethodResolutions.TryGetValue(page.Id, out var resolvedMethods))
            {
                // The user already decided what carries over via the VS extension's merge UI -
                // this covers both a same-name mismatch and a rename (HandleRenamedPageObjectAsync
                // above already deferred/cleaned up the old file in that case).
                customMethods = resolvedMethods;
            }
            else
            {
                // A method CustomMethodExtractor.Extract "recovers" is only trustworthy once (a)
                // the whole file's braces are still balanced - otherwise the depth-walk can latch
                // onto the wrong closing brace (e.g. the class's own) and report a clean,
                // matching-count "recovery" for a span that isn't actually that method - and (b)
                // the recovered text itself parses. Checking both here (not just when building a
                // PendingCustomMethodMerge) is what turns either failure into a caught mismatch
                // instead of silently re-emitting a misattributed/broken method as if it were fine.
                var fileIsBalanced = existingSource is null || CustomMethodExtractor.IsBalanced(existingSource);
                var recovered = existingSource is null || !fileIsBalanced
                    ? []
                    : CustomMethodExtractor.Extract(existingSource);
                var validated = recovered
                    .Where(method => CustomMethodSyntaxChecker.FindSyntaxErrors(method).Count == 0)
                    .ToList();
                customMethods = validated;
                if (existingSource is not null)
                {
                    var expectedCount = CustomMethodExtractor.CountMarkerOccurrences(existingSource);
                    if (validated.Count < expectedCount)
                    {
                        var backupPath = await BackupBeforeOverwriteAsync(
                            className, existingSource, cancellationToken);
                        warnings.Add(new CustomMethodRecoveryWarning(
                            className, expectedCount, validated.Count, backupPath));
                        var pending = await BuildPendingMergeAsync(
                            page.Id, className, existingSource, cancellationToken);
                        if (pending is not null)
                        {
                            pendingMerges.Add(pending);
                        }
                    }
                }
            }

            var pageCode = PageObjectCodeGenerator.Generate(
                className,
                project.Output.PageObjectsNamespace,
                descriptors,
                settings.TestExecution.DefaultStepTimeoutMs,
                customMethods,
                dynamicDescriptors);
            await WriteAtomicTextAsync(generatedPath, pageCode, cancellationToken);
            artifacts.Add(new GeneratedArtifact("pageObject", className, generatedPath, true));
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

        await SaveGenerationStateAsync(generationState, cancellationToken);

        return new WorkspaceGenerationResult(artifacts, warnings, pendingMerges);
    }

    /// <summary>
    /// A page renamed since it was last generated now maps to a different class name, leaving its
    /// old <c>&lt;OldName&gt;.cs</c> file behind under a name no page points to anymore -
    /// regeneration never reads or rewrites that file since it addresses pages by their current
    /// class name only. If the old file has no hand-written <c>[CliqCustomMethod]</c> content it's
    /// deleted (nothing to lose); otherwise it's left exactly where it is and reported as a
    /// warning rather than guessed at.
    /// </summary>
    /// <summary>
    /// Returns whether the rename conflict is now fully dealt with (the old file no longer
    /// exists) - false means it was left in place, unresolved, so the caller must keep pointing
    /// <c>generationState</c> at the old name until a future call resolves it.
    /// </summary>
    private async Task<bool> HandleRenamedPageObjectAsync(
        string pageObjectDirectory,
        string pageId,
        string previousClassName,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodResolutions,
        List<CustomMethodRecoveryWarning> warnings,
        List<PendingCustomMethodMerge> pendingMerges,
        CancellationToken cancellationToken)
    {
        var oldPath = Path.Combine(pageObjectDirectory, $"{previousClassName}.cs");
        if (!File.Exists(oldPath))
        {
            return true;
        }

        var oldSource = await File.ReadAllTextAsync(oldPath, cancellationToken);
        var customMethods = CustomMethodExtractor.Extract(oldSource);
        if (customMethods.Count == 0)
        {
            File.Delete(oldPath);
            return true;
        }

        if (customMethodResolutions is not null &&
            customMethodResolutions.TryGetValue(pageId, out var resolvedMethods))
        {
            // Resolved via the merge UI: whatever wasn't chosen to carry over goes to the waiting
            // list, and the stale renamed-away file can finally go - nothing of value is left in it.
            var candidates = BuildCandidates(CustomMethodExtractor.ExtractAll(oldSource));
            await DeferUnresolvedAsync(pageId, previousClassName, candidates, resolvedMethods, cancellationToken);
            File.Delete(oldPath);
            return true;
        }

        warnings.Add(new CustomMethodRecoveryWarning(previousClassName, customMethods.Count, 0, oldPath));
        var pending = await BuildPendingMergeAsync(pageId, previousClassName, oldSource, cancellationToken);
        if (pending is not null)
        {
            pendingMerges.Add(pending);
        }

        return false;
    }

    /// <summary>
    /// Builds the candidate list the VS extension's merge UI shows for one page's conflict -
    /// everything <see cref="CustomMethodExtractor.ExtractAll"/> found in the given source, merged
    /// with anything already deferred for the same page in the global waiting list (a fresh
    /// candidate wins over a stale deferred duplicate with the same signature). Returns null when
    /// there's nothing to show.
    /// </summary>
    private async Task<PendingCustomMethodMerge?> BuildPendingMergeAsync(
        string pageId,
        string pageObjectName,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var freshCandidates = BuildCandidates(CustomMethodExtractor.ExtractAll(sourceText));
        var waitingListCandidates = (await repository.ListPendingCustomMethodsAsync(cancellationToken))
            .Where(entry => string.Equals(entry.PageId, pageId, StringComparison.Ordinal))
            .Select(entry => new CustomMethodCandidate(
                entry.Signature,
                entry.SourceText,
                entry.HasSyntaxError,
                entry.HasSyntaxError
                    ? ["Previously deferred with a syntax error."]
                    : Array.Empty<string>()));

        var merged = freshCandidates
            .Concat(waitingListCandidates)
            .GroupBy(candidate => candidate.Signature, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        return merged.Count == 0 ? null : new PendingCustomMethodMerge(pageId, pageObjectName, merged);
    }

    private static IReadOnlyList<CustomMethodCandidate> BuildCandidates(IReadOnlyList<ExtractedSpan> spans)
    {
        var candidates = new List<CustomMethodCandidate>();
        foreach (var span in spans)
        {
            IReadOnlyList<string> syntaxErrors = span.ClosedCleanly
                ? CustomMethodSyntaxChecker.FindSyntaxErrors(span.SourceText)
                : ["The custom method's body could not be parsed (unbalanced braces or unsupported syntax)."];
            candidates.Add(new CustomMethodCandidate(
                ExtractSignature(span.SourceText), span.SourceText, syntaxErrors.Count > 0, syntaxErrors));
        }

        return candidates;
    }

    private static string ExtractSignature(string sourceText)
    {
        foreach (var line in sourceText.Split('\n'))
        {
            var trimmed = line.Trim('\r', ' ', '\t');
            if (trimmed.Length == 0 ||
                trimmed.StartsWith("///", StringComparison.Ordinal) ||
                (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
            {
                continue;
            }

            return trimmed.TrimEnd('{').TrimEnd();
        }

        return sourceText.Split('\n')[0].Trim();
    }

    /// <summary>
    /// Persists every candidate the user didn't choose to carry over into the global waiting list
    /// (preserving its existing entry Id if it was already deferred there, so a VS extension
    /// caller's cached reference doesn't go stale), and clears out any waiting-list entry for one
    /// that WAS carried over, since it's resolved now and shouldn't reappear.
    /// </summary>
    private async Task DeferUnresolvedAsync(
        string pageId,
        string pageObjectName,
        IReadOnlyList<CustomMethodCandidate> candidates,
        IReadOnlyList<string> resolvedMethods,
        CancellationToken cancellationToken)
    {
        var resolvedSet = new HashSet<string>(resolvedMethods, StringComparer.Ordinal);
        var existingWaitingList = await repository.ListPendingCustomMethodsAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var existing = existingWaitingList.FirstOrDefault(entry =>
                string.Equals(entry.PageId, pageId, StringComparison.Ordinal) &&
                string.Equals(entry.Signature, candidate.Signature, StringComparison.Ordinal));

            if (resolvedSet.Contains(candidate.SourceText))
            {
                if (existing is not null)
                {
                    await repository.DeletePendingCustomMethodAsync(existing.Id, cancellationToken);
                }

                continue;
            }

            await repository.SavePendingCustomMethodAsync(
                new PendingCustomMethodEntry(
                    existing?.Id ?? Guid.NewGuid().ToString("N"),
                    pageId,
                    pageObjectName,
                    candidate.Signature,
                    candidate.SourceText,
                    candidate.HasSyntaxError,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }

    /// <summary>
    /// Tracks the class name each page generated under on its last <see cref="GenerateAsync"/>
    /// call, so the next call can tell a rename happened (and clean up the stale file it leaves
    /// behind) instead of only ever seeing the current name with no memory of the previous one.
    /// </summary>
    private async Task<Dictionary<string, string>> LoadGenerationStateAsync(CancellationToken cancellationToken)
    {
        var path = GenerationStatePath;
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private async Task SaveGenerationStateAsync(
        Dictionary<string, string> state, CancellationToken cancellationToken)
    {
        var path = GenerationStatePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(state), cancellationToken);
    }

    private string GenerationStatePath => Path.Combine(repository.RootPath!, ".cliq", "generation-state.json");

    /// <summary>
    /// Copies a Page Object file's pre-regeneration contents to <c>.cliq/runs/custom-method-backups</c>
    /// before it gets overwritten - called only when <see cref="CustomMethodExtractor.CountMarkerOccurrences"/>
    /// disagrees with what <see cref="CustomMethodExtractor.Extract"/> actually recovered, so nothing
    /// is lost even though the mismatch means the cause isn't known.
    /// </summary>
    private async Task<string> BackupBeforeOverwriteAsync(
        string className,
        string existingSource,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(repository.RootPath!, ".cliq", "runs", "custom-method-backups");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{className}_{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.cs");
        await File.WriteAllTextAsync(path, existingSource, cancellationToken);
        return path;
    }

    internal static ElementDescriptor ToDescriptor(PageElement element, string testIdAttribute) =>
        new(
            element.Tag,
            element.Action switch
            {
                StepAction.Fill or StepAction.PressKey => GenerationKind.Fill,
                // Domain StepAction has no dedicated Radio value - a scanned radio group is just
                // StepAction.Select with a Name-strategy locator (the group's shared `name`
                // attribute; see PuppeteerPageDiscovery.ToAction/ToLocator), the only combination
                // that ever produces one. Without this check every radio group would generate as
                // an ordinary Select property using SelectElement, which throws on a non-<select>
                // element - the same distinction SeleniumTestRunner.ExecuteStep already makes for
                // live execution.
                StepAction.Select when element.Locator.Strategy == DomainLocator.Name => GenerationKind.Radio,
                // Same reasoning as Radio just above: StepAction has no dedicated MultiSelect
                // value either (a scanned <select multiple> is just StepAction.Select), so
                // PageElement.Multiple is the only surviving signal - see BuildElements, which
                // sets it from the scanner's own kind ("multiselect" vs "select").
                StepAction.Select when element.Multiple => GenerationKind.MultiSelect,
                StepAction.Select => GenerationKind.Select,
                // Same pattern again: a checkbox's Action is StepAction.Click (same as a plain
                // button), distinguished only by PageElement.Checkbox - see BuildElements.
                StepAction.Click when element.Checkbox => GenerationKind.Check,
                StepAction.AssertText or StepAction.AssertVisible => GenerationKind.Assert,
                _ => GenerationKind.Click,
            },
            element.Locator.Strategy switch
            {
                DomainLocator.Name => GenerationLocator.Name,
                DomainLocator.XPath => GenerationLocator.XPath,
                DomainLocator.TestId or DomainLocator.Css => GenerationLocator.CssSelector,
                _ => GenerationLocator.CssSelector,
            },
            element.Locator.Strategy == DomainLocator.TestId
                ? $"[{testIdAttribute}=\"{CssAttributeEscape(element.Locator.Value)}\"]"
                : element.Locator.Value,
            element.Name,
            element.ExpectedText,
            DataType: element.DataType);

    private static DynamicElementDescriptor ToDynamicDescriptor(DynamicElement element, string testIdAttribute) =>
        new(
            element.Action switch
            {
                StepAction.Fill => GenerationKind.Fill,
                StepAction.Select => GenerationKind.Select,
                StepAction.AssertText => GenerationKind.Assert,
                _ => GenerationKind.Click,
            },
            element.Strategy switch
            {
                DomainLocator.Name => GenerationLocator.Name,
                DomainLocator.XPath => GenerationLocator.XPath,
                DomainLocator.TestId or DomainLocator.Css => GenerationLocator.CssSelector,
                _ => GenerationLocator.CssSelector,
            },
            element.Strategy == DomainLocator.TestId
                ? $"[{testIdAttribute}=\"{CssAttributeEscape(element.LocatorTemplate)}\"]"
                : element.LocatorTemplate,
            DynamicMemberName(element),
            element.ParameterType,
            DataType: element.DataType);

    // Unlike PageElement.Name (already sanitized to a safe snake_case identifier at scan time -
    // see PuppeteerPageDiscovery), DynamicElement.Name is free text typed directly into the UI
    // form, so ToPascalCase's "split on _" assumption can't be trusted on it as-is (e.g. a space
    // would produce an invalid identifier like "Row button"). AppendDynamicFactory (via this
    // descriptor), AppendDynamicStep, and WorkspaceRepository's collision check (ValidateDynamicElements)
    // must all derive the exact same member name from the same DynamicElement, so this is the
    // single place that sanitizes it - internal rather than private for that reuse.
    internal static string DynamicMemberName(DynamicElement element) =>
        IdentifierNaming.Sanitize(element.Name, "dynamicElement");

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

        if (step.DynamicElementId is not null)
        {
            AppendDynamicStep(sb, step, pages, pageClassNames);
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
            case StepAction.Click when element.Checkbox:
                // Set{Name}(bool) is idempotent (only clicks if the current state differs from
                // the requested one) - see PageObjectCodeGenerator.AppendMethod's Check case. A
                // bare bool literal, not a quoted string - Set{Name} takes an actual bool.
                sb.AppendLine($"        {pageVariable}.Set{member}({(step.Value?.Value == "true" ? "true" : "false")});");
                break;
            case StepAction.Click:
                sb.AppendLine($"        {pageVariable}.Click{member}();");
                break;
            case StepAction.Fill:
            case StepAction.PressKey:
                // Fill/Select elements are ElementWrapper<TData> properties, not Fill{Name}(value)/
                // Select{Name}(value) methods - see PageObjectCodeGenerator's wrapped-element path.
                sb.AppendLine($"        {pageVariable}.{member}.Upload({value});");
                break;
            case StepAction.Select when element.Multiple:
                // ElementWrapper<string[]> for a multi-select - one value per selected option,
                // uploaded together (see PageObjectCodeGenerator.WrapperBody's MultiSelect case).
                sb.AppendLine($"        {pageVariable}.{member}.Upload({ValuesArrayExpression(step.Values)});");
                break;
            case StepAction.Select:
                sb.AppendLine($"        {pageVariable}.{member}.Upload({value});");
                break;
            case StepAction.AssertText:
                sb.AppendLine($"        Assert.That({pageVariable}.Get{member}Text(), Is.EqualTo({value}));");
                break;
            case StepAction.AssertVisible:
                sb.AppendLine($"        Assert.That(_driver.FindElement({ByExpression(element, testIdAttribute)}).Displayed, Is.True);");
                break;
        }
    }

    /// <summary>
    /// Mirrors <see cref="AppendStep"/> for a step targeting a <see cref="DynamicElement"/> -
    /// calls the same generated factory-method members (see
    /// <c>PageObjectCodeGenerator.AppendDynamicFactory</c>), just with the step's stored
    /// <see cref="TestStep.DynamicParameterValue"/> passed as the leading argument. Any problem
    /// (missing reference, unparsable/missing parameter value, unsupported action) becomes an
    /// <c>Assert.Fail(...)</c> in the generated test rather than aborting generation entirely,
    /// matching how <see cref="AppendStep"/> already handles a bad static reference.
    /// </summary>
    private static void AppendDynamicStep(
        StringBuilder sb,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> pageClassNames)
    {
        if (step.PageId is null
            || !pages.TryGetValue(step.PageId, out var page)
            || !pageClassNames.TryGetValue(step.PageId, out var pageClassName))
        {
            sb.AppendLine($"        Assert.Fail({Literal($"Step '{step.Label}' has no valid page reference.")});");
            return;
        }

        var dynamicElement = (page.DynamicElements ?? []).SingleOrDefault(item => item.Id == step.DynamicElementId);
        if (dynamicElement is null)
        {
            sb.AppendLine($"        Assert.Fail({Literal($"Dynamic element '{step.DynamicElementId}' was not found.")});");
            return;
        }

        if (!TryDynamicParameterLiteral(dynamicElement, step.DynamicParameterValue, out var paramLiteral))
        {
            sb.AppendLine($"        Assert.Fail({Literal($"Step '{step.Label}' has no valid parameter value for '{dynamicElement.Name}'.")});");
            return;
        }

        var member = IdentifierNaming.ToPascalCase(DynamicMemberName(dynamicElement));
        var pageVariable = Variable(pageClassName);
        var value = ValueExpression(step.Value);
        switch (step.Action)
        {
            case StepAction.Click:
                sb.AppendLine($"        {pageVariable}.Click{member}({paramLiteral});");
                break;
            case StepAction.Fill:
            case StepAction.PressKey:
            case StepAction.Select:
                sb.AppendLine($"        {pageVariable}.{member}({paramLiteral}).Upload({value});");
                break;
            case StepAction.AssertText:
                sb.AppendLine($"        Assert.That({pageVariable}.Get{member}Text({paramLiteral}), Is.EqualTo({value}));");
                break;
            default:
                sb.AppendLine($"        Assert.Fail({Literal($"Step '{step.Label}' uses an action dynamic elements do not support.")});");
                break;
        }
    }

    private static bool TryDynamicParameterLiteral(DynamicElement element, string? value, out string literal)
    {
        if (string.IsNullOrEmpty(value))
        {
            literal = "";
            return false;
        }

        if (element.ParameterType == "string")
        {
            literal = Literal(value);
            return true;
        }

        if (!int.TryParse(value, out _))
        {
            literal = "";
            return false;
        }

        literal = value;
        return true;
    }

    private static string ByExpression(PageElement element, string testIdAttribute)
    {
        var locatorValue = element.Locator.Strategy == DomainLocator.TestId
            ? $"[{testIdAttribute}=\"{CssAttributeEscape(element.Locator.Value)}\"]"
            : element.Locator.Value;
        var value = Literal(locatorValue);
        return element.Locator.Strategy switch
        {
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

    private static string ValuesArrayExpression(IReadOnlyList<StepValue>? values) =>
        $"new[] {{ {string.Join(", ", (values ?? []).Select(item => ValueExpression(item)))} }}";

    private static string NormalizeSecretKey(string value) =>
        string.Concat(value.Select(character => char.IsLetterOrDigit(character)
            ? char.ToUpperInvariant(character)
            : '_'));

    private static string Literal(string value) => PageObjectCodeGenerator.ToCSharpStringLiteral(value);
    private static string Variable(string className) => char.ToLowerInvariant(className[0]) + className[1..];
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
