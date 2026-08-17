using System.Collections.Concurrent;
using System.Text.Json;
using Cliq.Application;

namespace Cliq.Infrastructure;

/// <summary>
/// Exclusivity for a single .cliq workspace directory, both across processes (via a lock file
/// under the workspace) and across every <see cref="WorkspaceRepository"/> instance within this
/// process (via a static registry, keyed by the lock file's full path) - extracted out of
/// <see cref="WorkspaceRepository"/> so its entity CRUD/validation responsibilities aren't
/// entangled with this separate concern of "is anyone else already using this workspace".
/// </summary>
internal static class WorkspaceLeaseManager
{
    private static readonly ConcurrentDictionary<string, WorkspaceLeaseOwner> ProcessLeases =
        new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    public static async Task<WorkspaceLease> AcquireAsync(
        string cliqDirectory,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var lockPath = Path.GetFullPath(Path.Combine(cliqDirectory, "workspace.lock"));
        var owner = new WorkspaceLeaseOwner(
            NormalizeHostKind(Environment.GetEnvironmentVariable("CLIQ_HOST_KIND")),
            Environment.ProcessId,
            DateTimeOffset.UtcNow);
        if (!ProcessLeases.TryAdd(lockPath, owner))
        {
            ProcessLeases.TryGetValue(lockPath, out var existingOwner);
            throw new WorkspaceInUseException(existingOwner);
        }

        FileStream? stream = null;
        try
        {
            try
            {
                stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.Read,
                    bufferSize: 4_096,
                    FileOptions.Asynchronous);
            }
            catch (IOException)
            {
                throw new WorkspaceInUseException(await ReadLeaseOwnerAsync(lockPath, jsonOptions, cancellationToken));
            }

            stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream, owner, jsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            return new WorkspaceLease(lockPath, stream);
        }
        catch
        {
            try
            {
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }
            }
            finally
            {
                ProcessLeases.TryRemove(lockPath, out _);
            }

            throw;
        }
    }

    public sealed class WorkspaceLease(string lockPath, FileStream stream) : IDisposable
    {
        private FileStream? _stream = stream;

        public void Dispose()
        {
            var ownedStream = Interlocked.Exchange(ref _stream, null);
            if (ownedStream is null)
            {
                return;
            }

            try
            {
                ownedStream.Dispose();
            }
            finally
            {
                ProcessLeases.TryRemove(lockPath, out _);
            }
        }
    }

    private static async Task<WorkspaceLeaseOwner?> ReadLeaseOwnerAsync(
        string lockPath,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                lockPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4_096,
                FileOptions.Asynchronous);
            var owner = await JsonSerializer.DeserializeAsync<WorkspaceLeaseOwner>(
                stream,
                jsonOptions,
                cancellationToken);
            return owner is null
                ? null
                : owner with
                {
                    HostKind = NormalizeHostKind(owner.HostKind),
                    ProcessId = Math.Max(0, owner.ProcessId),
                };
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeHostKind(string? hostKind)
    {
        if (string.IsNullOrWhiteSpace(hostKind))
        {
            return "unknown";
        }

        var normalized = new string(hostKind
            .Trim()
            .Take(64)
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        return string.IsNullOrEmpty(normalized) ? "unknown" : normalized;
    }
}
