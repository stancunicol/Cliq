using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cliq.UnitTests;

/// <summary>
/// The same wire-format convention (camelCase string enums) Cliq.Api actually serializes with -
/// centralized here, analogous to Cliq.IntegrationTests.IntegrationJson, so the handful of tests
/// that round-trip JSON directly all agree on it instead of each redefining their own copy.
/// </summary>
internal static class TestJson
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
