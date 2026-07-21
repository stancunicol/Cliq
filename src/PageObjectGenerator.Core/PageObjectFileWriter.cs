using System.Text.Json;
using System.Text.Json.Serialization;

namespace PageObjectGenerator.Core;

public static class PageObjectFileWriter
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static (string PageObjectPath, string TestPath, string MetadataPath) Write(string outDir, GenerationResult result)
    {
        // Defense in depth: PageObjectWorkflow.Generate already rejects an unsafe
        // ClassName, but this is the one place that actually touches the
        // filesystem, so it must never trust its input either.
        if (!IdentifierNaming.IsSafeIdentifier(result.ClassName))
        {
            throw new ArgumentException($"'{result.ClassName}' is not a valid C# class name.", nameof(result));
        }

        Directory.CreateDirectory(outDir);
        var pageObjectPath = Path.Combine(outDir, $"{result.ClassName}.cs");
        var testPath = Path.Combine(outDir, $"{result.ClassName}Tests.cs");
        var metadataPath = Path.Combine(outDir, $"{result.ClassName}.meta.json");

        File.WriteAllText(pageObjectPath, result.PageObjectCode);
        File.WriteAllText(testPath, result.TestCode);

        var metadata = new GenerationMetadata(result.ClassName, result.Url, result.Browser, result.Namespace, result.Elements);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, MetadataJsonOptions));

        return (pageObjectPath, testPath, metadataPath);
    }

    /// <summary>Base class names (e.g. "LoginPage") that have a generated pair in <paramref name="outDir"/>.</summary>
    public static IEnumerable<string> ListGenerated(string outDir)
    {
        if (!Directory.Exists(outDir))
        {
            return [];
        }

        return Directory.EnumerateFiles(outDir, "*.cs")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && !name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }

    // className here comes straight from a caller-supplied route parameter (the
    // API's GET/RUN endpoints), never from our own generation flow - it has to be
    // validated before it can touch Path.Combine, or a value like "../../secrets"
    // (or a rooted path, which Path.Combine will happily switch to entirely) lets
    // a caller read or overwrite arbitrary files outside outDir.
    public static (string? PageObjectCode, string? TestCode) TryRead(string outDir, string className)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            return (null, null);
        }

        var pageObjectPath = Path.Combine(outDir, $"{className}.cs");
        var testPath = Path.Combine(outDir, $"{className}Tests.cs");
        if (!File.Exists(pageObjectPath))
        {
            return (null, null);
        }

        var testCode = File.Exists(testPath) ? File.ReadAllText(testPath) : null;
        return (File.ReadAllText(pageObjectPath), testCode);
    }

    public static GenerationMetadata? TryReadMetadata(string outDir, string className)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            return null;
        }

        var metadataPath = Path.Combine(outDir, $"{className}.meta.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        GenerationMetadata? metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<GenerationMetadata>(File.ReadAllText(metadataPath), MetadataJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        // A .meta.json written by an older schema (e.g. before ElementDescriptor
        // gained Strategy/LocatorValue) deserializes "successfully" but leaves
        // LocatorValue null/empty on every element, since JSON has no field for
        // it - silently producing a By.Id(null) crash deep inside Selenium
        // instead of a clear error. Treat that as "not found" so the caller
        // regenerates instead of running against corrupt data.
        if (metadata is null || metadata.Elements.Any(e => string.IsNullOrEmpty(e.LocatorValue)))
        {
            return null;
        }

        return metadata;
    }

    /// <summary>Deletes a previously generated class's .cs/Tests.cs/.meta.json. Returns false if it didn't exist.</summary>
    public static bool Delete(string outDir, string className)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            return false;
        }

        var pageObjectPath = Path.Combine(outDir, $"{className}.cs");
        var testPath = Path.Combine(outDir, $"{className}Tests.cs");
        var metadataPath = Path.Combine(outDir, $"{className}.meta.json");

        var existed = File.Exists(pageObjectPath);

        if (existed)
        {
            File.Delete(pageObjectPath);
        }

        if (File.Exists(testPath))
        {
            File.Delete(testPath);
        }

        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        return existed;
    }
}
