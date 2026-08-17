using System.Text.Json;

namespace Cliq.Infrastructure;

/// <summary>
/// Writes a file by first writing to a sibling ".tmp" file and renaming it into place, so a
/// concurrent reader never observes a partially-written file. Serializes every write made through
/// the same instance with an internal gate, and always deletes the temp file if anything goes
/// wrong before the rename.
/// </summary>
internal sealed class AtomicFileWriter : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public void Dispose() => _gate.Dispose();

    public Task WriteJsonAsync<T>(
        string path,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken) =>
        WriteAsync(
            path,
            stream => JsonSerializer.SerializeAsync(stream, value, options, cancellationToken),
            cancellationToken);

    public Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken) =>
        WriteAsync(
            path,
            async stream =>
            {
                await using var writer = new StreamWriter(stream, leaveOpen: true);
                await writer.WriteAsync(contents.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
            },
            cancellationToken);

    private async Task WriteAsync(string path, Func<Stream, Task> writeBody, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                    temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4_096, FileOptions.Asynchronous))
                {
                    await writeBody(stream);
                    await stream.FlushAsync(cancellationToken);
                }

                File.Move(temporary, path, true);
            }
            catch
            {
                File.Delete(temporary);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
