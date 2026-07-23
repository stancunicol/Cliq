using PageObjectGenerator.Core;

if (args.Length < 1 || args[0] is "-h" or "--help")
{
    Console.WriteLine("""
        Usage: PageObjectGenerator.Cli <url> [options]

        Options:
          --browser chrome|edge   Browser to drive (default: chrome)
          --headless              Run the browser without a visible window
          --class <Name>          Page Object class name (default: derived from the URL)
          --namespace <Ns>        Namespace for generated code (default: Generated)
          --out <dir>             Output directory (default: GeneratedPages)
        """);
    return args.Length < 1 ? 1 : 0;
}

var url = args[0];
var browser = BrowserKind.Chrome;
var headless = false;
string? className = null;
var @namespace = "Generated";
var outDir = SolutionPaths.DefaultOutputDirectory;

for (var i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--browser":
            if (!TryNextArg(args, ref i, "--browser", out var browserValue)) return 1;
            browser = PageObjectWorkflow.ParseBrowserKind(browserValue);
            break;
        case "--headless":
            headless = true;
            break;
        case "--class":
            if (!TryNextArg(args, ref i, "--class", out var classValue)) return 1;
            className = classValue;
            break;
        case "--namespace":
            if (!TryNextArg(args, ref i, "--namespace", out var namespaceValue)) return 1;
            @namespace = namespaceValue;
            break;
        case "--out":
            if (!TryNextArg(args, ref i, "--out", out var outValue)) return 1;
            outDir = outValue;
            break;
        default:
            Console.Error.WriteLine($"Unknown option '{args[i]}'");
            return 1;
    }
}

// Bounds-checks the value following a flag (e.g. "--browser" as the last
// argument, with nothing after it) instead of letting args[++i] throw an
// unhandled IndexOutOfRangeException.
static bool TryNextArg(string[] args, ref int i, string option, out string value)
{
    if (i + 1 >= args.Length)
    {
        Console.Error.WriteLine($"Missing value for {option}");
        value = "";
        return false;
    }

    value = args[++i];
    return true;
}

className ??= PageObjectWorkflow.DeriveClassName(url);

GenerationResult result;
try
{
    Console.WriteLine($"Launching {browser} ({(headless ? "headless" : "headed")})...");
    result = PageObjectWorkflow.Generate(url, browser, headless, className, @namespace);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Console.WriteLine($"Found {result.ElementCount} interactive element(s).");
if (result.ScreenshotPath is not null)
{
    Console.WriteLine($"Baseline screenshot: {result.ScreenshotPath}");
}

var written = PageObjectFileWriter.Write(outDir, result);
Console.WriteLine($"Wrote {written.PageObjectPath}");
Console.WriteLine($"Wrote {written.TestPath}");
Console.WriteLine($"Wrote {written.MetadataPath}");
Console.WriteLine(written.CustomFileCreated
    ? $"Wrote {written.CustomPath} (safe to add your own methods here - never regenerated)"
    : $"Kept {written.CustomPath} (already existed, left untouched)");

return 0;
