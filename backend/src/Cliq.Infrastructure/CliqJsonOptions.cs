using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cliq.Infrastructure;

/// <summary>
/// Shared System.Text.Json configuration for workspace/domain files the backend reads or writes
/// (project, settings, pages, tests, scan reviews, secrets, the lease file) - centralized so every
/// reader and writer agrees on shape (Web camelCase, enum names rather than ordinals) instead of
/// each file declaring its own copy that can silently drift from the others.
/// </summary>
public static class CliqJsonOptions
{
    public static readonly JsonSerializerOptions Indented = Create(writeIndented: true);
    public static readonly JsonSerializerOptions Compact = Create(writeIndented: false);

    private static JsonSerializerOptions Create(bool writeIndented) => new(JsonSerializerDefaults.Web)
    {
        WriteIndented = writeIndented,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
