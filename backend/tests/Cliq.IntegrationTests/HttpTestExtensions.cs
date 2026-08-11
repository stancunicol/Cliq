using System.Net.Http.Json;

namespace Cliq.IntegrationTests;

internal static class HttpTestExtensions
{
    internal static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string path, T value) =>
        client.PostAsJsonAsync(path, value, IntegrationJson.Options);

    internal static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string path, T value) =>
        client.PutAsJsonAsync(path, value, IntegrationJson.Options);

    internal static async Task<T> ReadRequiredJsonAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(IntegrationJson.Options);
        return value ?? throw new InvalidOperationException(
            $"Response from '{response.RequestMessage?.RequestUri}' did not contain {typeof(T).Name} JSON.");
    }

    internal static async Task<T> GetRequiredJsonAsync<T>(this HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<T>();
    }
}
