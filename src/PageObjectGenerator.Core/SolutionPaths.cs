namespace PageObjectGenerator.Core;

/// <summary>
/// ASP.NET Core resets the process's current directory to the project's
/// ContentRootPath at startup, while a plain console app leaves it as
/// whatever the shell's CWD was - so the CLI and the API would otherwise
/// resolve a relative "GeneratedPages" default to two different places.
/// Anchoring to the solution file instead makes the default stable and
/// identical for every host.
/// </summary>
public static class SolutionPaths
{
    public static string DefaultOutputDirectory => Path.Combine(FindRepoRoot(), "GeneratedPages");

    public static string DefaultScreenshotDirectory => Path.Combine(FindRepoRoot(), "Screenshots");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.sln").Any())
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
