using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Api
{
    internal static class CliqJson
    {
        public static readonly JsonSerializerOptions Options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter(new CliqEnumNamingPolicy()));
            return options;
        }

        private sealed class CliqEnumNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) =>
                name == nameof(LocatorStrategy.XPath)
                    ? "xpath"
                    : JsonNamingPolicy.CamelCase.ConvertName(name);
        }
    }
}
