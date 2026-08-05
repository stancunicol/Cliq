using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using DomainBrowser = Cliq.Domain.BrowserKind;
using DomainLocator = Cliq.Domain.LocatorStrategy;

namespace Cliq.Infrastructure;

public sealed class PuppeteerPageDiscovery(WorkspaceRepository repository) : IPageDiscovery
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // Separate from JsonOptions above (which only deserializes the scanner's own JS output and
    // is never read by a person): this report is meant to be opened and read, so it's indented
    // and spells out the locator strategy name instead of its underlying enum ordinal.
    private static readonly JsonSerializerOptions DiffReportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<PageScanResult> ScanAsync(
        string url,
        string name,
        DomainBrowser browser,
        bool headless,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new WorkspaceValidationException("Only absolute HTTP and HTTPS URLs can be scanned.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var root = repository.RootPath ?? throw new WorkspaceNotOpenException();
        var settings = await repository.GetSettingsAsync(cancellationToken);
        var screenshotDirectory = Path.Combine(root, ".cliq", "runs", "discovery");
        var className = PageObjectWorkflow.DeriveClassName(url);
        var pageId = Slug(name);
        var outPath = Path.Combine(Path.GetTempPath(), $"cliq-scan-{Guid.NewGuid():N}.json");

        try
        {
            var scanOutput = await RunScanProcessAsync(
                url, browser, headless, screenshotDirectory, className,
                settings.ElementIdentification, settings.Scanning, outPath, cancellationToken);

            var freshElements = BuildElements(scanOutput.Elements, settings.ElementIdentification.TestIdAttribute);
            var previous = await repository.GetPageAsync(pageId, cancellationToken);
            var (elements, report, newElements) = ElementReconciler.Reconcile(freshElements, previous?.Elements ?? []);

            var page = new PageDefinition(
                PageDefinition.CurrentSchemaVersion,
                pageId,
                name,
                url,
                elements,
                DateTimeOffset.UtcNow);

            // Only worth producing once there's a previous scan to diff against, and only when
            // something actually moved - a clean re-scan writes nothing, per the "minimal
            // information surfaced" principle this replaced self-healing with.
            var missingElements = elements.Where(element => element.NotFound).ToList();
            var scannedAt = DateTimeOffset.UtcNow;
            var locatorDiff = previous is not null && (newElements.Count > 0 || missingElements.Count > 0)
                ? await WriteLocatorDiffReportAsync(root, pageId, scannedAt, newElements, missingElements, cancellationToken)
                : null;

            return new PageScanResult(page, report, locatorDiff);
        }
        finally
        {
            try
            {
                File.Delete(outPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a temp file; leaving it behind is harmless.
            }
        }
    }

    internal static async Task<LocatorDiffReport> WriteLocatorDiffReportAsync(
        string root,
        string pageId,
        DateTimeOffset scannedAt,
        IReadOnlyList<PageElement> newElements,
        IReadOnlyList<PageElement> missingElements,
        CancellationToken cancellationToken)
    {
        var diff = new LocatorDiffReport(
            pageId,
            scannedAt,
            [.. newElements.Select(ToDiffEntry)],
            [.. missingElements.Select(ToDiffEntry)]);

        var directory = Path.Combine(root, ".cliq", "runs", "diffs");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{pageId}_{scannedAt:yyyyMMddTHHmmssfffZ}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(diff, DiffReportJsonOptions), cancellationToken);

        return diff;
    }

    private static LocatorDiffEntry ToDiffEntry(PageElement element) =>
        new(element.Id, element.Name, element.Tag, element.Locator);

    private static async Task<ScanOutput> RunScanProcessAsync(
        string url,
        DomainBrowser browser,
        bool headless,
        string screenshotDirectory,
        string label,
        ElementIdentificationSettings identification,
        ScanningSettings scanning,
        string outPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveNodeExecutable(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(ResolveScannerScript());
        startInfo.ArgumentList.Add("--url");
        startInfo.ArgumentList.Add(url);
        startInfo.ArgumentList.Add("--out");
        startInfo.ArgumentList.Add(outPath);
        startInfo.ArgumentList.Add("--browser");
        startInfo.ArgumentList.Add(browser == DomainBrowser.Edge ? "edge" : "chrome");
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add(headless ? "true" : "false");
        if (scanning.CaptureScreenshot)
        {
            startInfo.ArgumentList.Add("--screenshot-dir");
            startInfo.ArgumentList.Add(screenshotDirectory);
        }

        startInfo.ArgumentList.Add("--label");
        startInfo.ArgumentList.Add(label);
        startInfo.ArgumentList.Add("--locator-config");
        startInfo.ArgumentList.Add(JsonSerializer.Serialize(
            new
            {
                testIdAttribute = identification.TestIdAttribute,
                strategyOrder = identification.StrategyOrder,
                ignoreIdPatterns = identification.IgnoreIdPatterns,
            }));
        startInfo.ArgumentList.Add("--load-timeout-ms");
        startInfo.ArgumentList.Add(scanning.LoadTimeoutMs.ToString());
        startInfo.ArgumentList.Add("--network-idle-timeout-ms");
        startInfo.ArgumentList.Add(scanning.NetworkIdleGraceMs.ToString());

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdoutDrain = process.StandardOutput.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stderr = await stderrTask;
        await stdoutDrain;

        if (process.ExitCode != 0)
        {
            throw new WorkspaceValidationException(
                string.IsNullOrWhiteSpace(stderr) ? "The Puppeteer scan failed." : stderr.Trim());
        }

        if (!File.Exists(outPath))
        {
            throw new WorkspaceValidationException("The Puppeteer scan did not produce any output.");
        }

        var json = await File.ReadAllTextAsync(outPath, cancellationToken);
        return JsonSerializer.Deserialize<ScanOutput>(json, JsonOptions)
            ?? throw new WorkspaceValidationException("The Puppeteer scan returned invalid output.");
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the check and the kill attempt.
        }
    }

    private static string ResolveNodeExecutable() =>
        Environment.GetEnvironmentVariable("CLIQ_NODE_PATH") is { Length: > 0 } configured
            ? configured
            : "node";

    private static string ResolveScannerScript()
    {
        var overridePath = Environment.GetEnvironmentVariable("CLIQ_PUPPETEER_SCANNER_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return Path.Combine(dir.FullName, "backend", "tools", "puppeteer-scanner", "scan.js");
            }
        }

        throw new InvalidOperationException(
            "Could not locate the repository root to find the Puppeteer scanner script. Set CLIQ_PUPPETEER_SCANNER_PATH.");
    }

    internal static IReadOnlyList<PageElement> BuildElements(IReadOnlyList<RawElement> rawElements, string testIdAttribute)
    {
        var elements = new List<PageElement>();
        var usedNames = new Dictionary<string, int>();
        // Defensive: the scanner only ever accepts a testId/xpath candidate after verifying it
        // resolves to exactly one DOM node (see extract-elements.js's isUniqueMatch/
        // isUniqueXPathMatch), so two distinct elements should never legitimately share a
        // locator - but if they somehow did, keep only the first rather than emitting duplicate
        // Page Object entries for what would read as "the same" element.
        var seenLocators = new HashSet<string>();

        for (var i = 0; i < rawElements.Count; i++)
        {
            var el = rawElements[i];
            if (!seenLocators.Add($"{el.Tag}|{el.Strategy}|{el.LocatorValue}"))
            {
                continue;
            }

            var baseName = el.Kind == "radio"
                ? el.GroupName
                : el.TestId ?? el.Id ?? el.Name ?? el.Placeholder ?? el.Text;
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"{el.Tag}_{i}";
            }

            var name = IdentifierNaming.Sanitize(baseName, $"{el.Tag}_{i}");

            usedNames.TryGetValue(name, out var seen);
            usedNames[name] = seen + 1;
            if (seen > 0)
            {
                name = $"{name}_{seen + 1}";
            }

            elements.Add(new PageElement(
                $"{Slug(name)}-{i + 1}",
                name,
                el.Tag,
                ToAction(el.Kind),
                ToLocator(el.Strategy, el.LocatorValue, name, testIdAttribute),
                el.AssertText,
                DataType: ToDataType(el.Kind, el.InputType)));
        }

        return elements;
    }

    private static StepAction ToAction(string kind) => kind switch
    {
        "fill" => StepAction.Fill,
        "click" => StepAction.Click,
        "select" or "multiselect" or "radio" => StepAction.Select,
        "check" => StepAction.Click,
        "assert" => StepAction.AssertText,
        _ => throw new InvalidOperationException($"Unknown element kind '{kind}'"),
    };

    // Only "fill" elements carry a meaningful data type - everything else defaults to "string"
    // and PageObjectCodeGenerator ignores it for those kinds anyway (Select/MultiSelect decide
    // their own wrapper type from the action, not from this field).
    private static string ToDataType(string kind, string? inputType)
    {
        if (kind != "fill")
        {
            return "string";
        }

        return (inputType ?? "text").ToLowerInvariant() switch
        {
            "number" or "range" => "int",
            "date" or "datetime-local" or "month" or "week" => "DateTime",
            _ => "string",
        };
    }

    private static Locator ToLocator(string strategy, string value, string name, string testIdAttribute)
    {
        var testIdPrefix = $"[{testIdAttribute}=\"";
        if (strategy == "css"
            && value.StartsWith(testIdPrefix, StringComparison.Ordinal)
            && value.EndsWith("\"]", StringComparison.Ordinal))
        {
            return new Locator(DomainLocator.TestId, value[testIdPrefix.Length..^2], name);
        }

        return new Locator(
            strategy switch
            {
                "name" => DomainLocator.Name,
                "xpath" => DomainLocator.XPath,
                _ => DomainLocator.Css,
            },
            value,
            name);
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-');
        var slug = string.Join(string.Empty, chars).Trim('-');
        return string.IsNullOrEmpty(slug) ? "page" : slug;
    }

    private sealed class ScanOutput
    {
        public List<RawElement> Elements { get; set; } = [];
        public string? ScreenshotPath { get; set; }
    }

    internal sealed class RawElement
    {
        public string Tag { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Strategy { get; set; } = "";
        public string LocatorValue { get; set; } = "";
        public string? GroupName { get; set; }
        public string? TestId { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Placeholder { get; set; }
        public string? Text { get; set; }
        public string? AssertText { get; set; }
        public string? InputType { get; set; }
    }
}
