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
            browser = PageObjectWorkflow.ParseBrowserKind(args[++i]);
            break;
        case "--headless":
            headless = true;
            break;
        case "--class":
            className = args[++i];
            break;
        case "--namespace":
            @namespace = args[++i];
            break;
        case "--out":
            outDir = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown option '{args[i]}'");
            return 1;
    }
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
