using Cliq.Application;
using Cliq.Domain;
using GenerationIdentifierNaming = Cliq.Infrastructure.Generation.IdentifierNaming;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository
{
    public Task SaveRunAsync(TestRun run, CancellationToken cancellationToken)
    {
        EnsureOpen();
        var runDirectory = Path.Combine(RunsDirectory, SafeId(run.Id));
        Directory.CreateDirectory(runDirectory);
        return WriteAtomicAsync(Path.Combine(runDirectory, "run.json"), run, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingCustomMethodEntry>> ListPendingCustomMethodsAsync(CancellationToken cancellationToken)
    {
        EnsureOpen();
        return await ReadOptionalAsync<List<PendingCustomMethodEntry>>(PendingCustomMethodsPath, cancellationToken)
            ?? [];
    }

    // Upserts by (PageId, Signature) rather than Id, so re-deferring the same method (e.g. it's
    // offered again in a later conflict for the same page) replaces its entry instead of
    // duplicating it - Id only exists to give the VS extension a stable key for a discard action.
    public async Task SavePendingCustomMethodAsync(PendingCustomMethodEntry entry, CancellationToken cancellationToken)
    {
        EnsureOpen();
        var entries = (await ListPendingCustomMethodsAsync(cancellationToken)).ToList();
        var existingIndex = entries.FindIndex(item =>
            string.Equals(item.PageId, entry.PageId, StringComparison.Ordinal) &&
            string.Equals(item.Signature, entry.Signature, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            entries[existingIndex] = entry;
        }
        else
        {
            entries.Add(entry);
        }

        await WriteAtomicAsync(PendingCustomMethodsPath, entries, cancellationToken);
    }

    public async Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken)
    {
        EnsureOpen();
        var entries = (await ListPendingCustomMethodsAsync(cancellationToken))
            .Where(item => !string.Equals(item.Id, id, StringComparison.Ordinal))
            .ToList();
        await WriteAtomicAsync(PendingCustomMethodsPath, entries, cancellationToken);
    }

    public string? GetScreenshotPath(string runId, string fileName)
    {
        EnsureOpen();
        if (!IdPattern().IsMatch(runId) || !GenerationIdentifierNaming.IsSafeFileName(fileName))
        {
            return null;
        }

        var path = Path.Combine(RunsDirectory, runId, fileName);
        return File.Exists(path) ? path : null;
    }

    // Discovery screenshots (see PuppeteerPageDiscovery.ScanAsync) all land in one flat
    // "discovery" directory rather than a per-run one - a scan isn't a run, it has no id to scope
    // by - so this only needs the same filename safety check GetScreenshotPath already applies.
    public string? GetDiscoveryScreenshotPath(string fileName)
    {
        EnsureOpen();
        if (!GenerationIdentifierNaming.IsSafeFileName(fileName))
        {
            return null;
        }

        var path = Path.Combine(RunsDirectory, "discovery", fileName);
        return File.Exists(path) ? path : null;
    }

    internal string GetContainedPath(string relativeDirectory)
    {
        EnsureOpen();
        ValidateRelativeDirectory(relativeDirectory, nameof(relativeDirectory));
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(RootPath!));
        var candidate = Path.GetFullPath(Path.Combine(root, relativeDirectory));

        // Path.GetRelativePath (rather than a raw StartsWith) handles trailing separators and,
        // on Windows, compares case-insensitively - a plain Ordinal StartsWith check previously
        // used here could false-positive-reject a valid path whose casing merely differed from
        // RootPath's, which the filesystem itself would treat as the same location.
        var relative = Path.GetRelativePath(root, candidate);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new WorkspaceValidationException(
                $"Configured output directory escapes the workspace. Workspace root: '{root}'. " +
                $"Resolved output directory: '{candidate}'.");
        }

        return candidate;
    }

    public async Task<string> GetGeneratedPageObjectPathAsync(PageDefinition page, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        var directory = GetContainedPath(snapshot.Project.Output.PageObjectsDirectory);
        var className = GenerationIdentifierNaming.ToPageObjectClassName(page.Name);
        return Path.Combine(directory, $"{className}.cs");
    }
}
