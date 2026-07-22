using System.Text.Json;
using System.Text.Json.Serialization;

namespace PageObjectGenerator.Core;

public sealed record WriteResult(
    string PageObjectPath, string TestPath, string MetadataPath, string CustomPath, bool CustomFileCreated);

public static class PageObjectFileWriter
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static WriteResult Write(string outDir, GenerationResult result)
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
        var customPath = Path.Combine(outDir, $"{result.ClassName}.Custom.cs");

        File.WriteAllText(pageObjectPath, result.PageObjectCode);
        File.WriteAllText(testPath, result.TestCode);

        var metadata = new GenerationMetadata(result.ClassName, result.Url, result.Browser, result.Namespace, result.Elements);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, MetadataJsonOptions));

        // Written once, on first generation, then left alone forever - this is
        // the file a developer is meant to add hand-written methods to. If it
        // already exists (from a previous generation, possibly hand-edited
        // since), regenerating must never touch it.
        var customFileCreated = !File.Exists(customPath);
        if (customFileCreated)
        {
            File.WriteAllText(customPath, PageObjectCodeGenerator.GenerateCustomStub(result.ClassName, result.Namespace));
        }

        return new WriteResult(pageObjectPath, testPath, metadataPath, customPath, customFileCreated);
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
            .Where(name => name is not null && !name.EndsWith("Tests", StringComparison.Ordinal) && !name.EndsWith(".Custom", StringComparison.Ordinal))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }

    // className here comes straight from a caller-supplied route parameter (the
    // API's GET/RUN endpoints), never from our own generation flow - it has to be
    // validated before it can touch Path.Combine, or a value like "../../secrets"
    // (or a rooted path, which Path.Combine will happily switch to entirely) lets
    // a caller read or overwrite arbitrary files outside outDir.
    public static (string? PageObjectCode, string? TestCode, string? CustomCode) TryRead(string outDir, string className)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            return (null, null, null);
        }

        var pageObjectPath = Path.Combine(outDir, $"{className}.cs");
        var testPath = Path.Combine(outDir, $"{className}Tests.cs");
        var customPath = Path.Combine(outDir, $"{className}.Custom.cs");
        if (!File.Exists(pageObjectPath))
        {
            return (null, null, null);
        }

        var testCode = File.Exists(testPath) ? File.ReadAllText(testPath) : null;
        var customCode = File.Exists(customPath) ? File.ReadAllText(customPath) : null;
        return (File.ReadAllText(pageObjectPath), testCode, customCode);
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

    /// <summary>
    /// Deletes a previously generated class's .cs/Tests.cs/.meta.json. Returns
    /// false if it didn't exist. Deliberately leaves {className}.Custom.cs
    /// alone - deleting a developer's hand-written code because the generated
    /// counterpart was removed would be exactly the kind of silent data loss
    /// the whole partial-class split exists to prevent.
    /// </summary>
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
