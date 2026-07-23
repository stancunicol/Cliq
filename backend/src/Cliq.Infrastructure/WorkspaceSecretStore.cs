using System.Text.Json;
using Cliq.Application;

namespace Cliq.Infrastructure;

public sealed class WorkspaceSecretStore(WorkspaceRepository repository) : ISecretStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(CancellationToken cancellationToken)
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public async Task SaveAsync(
        IReadOnlyDictionary<string, string> secrets,
        CancellationToken cancellationToken)
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var merged = new Dictionary<string, string>(
            await ReadAsync(cancellationToken),
            StringComparer.Ordinal);
        foreach (var (key, value) in secrets)
        {
            merged[key] = value;
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, merged, JsonOptions, cancellationToken);
    }

    private string GetPath() =>
        Path.Combine(repository.RootPath ?? throw new WorkspaceNotOpenException(), ".cliq", "secrets.local.json");
}
