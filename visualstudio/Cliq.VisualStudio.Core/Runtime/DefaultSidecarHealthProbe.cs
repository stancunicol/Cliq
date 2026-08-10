using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cliq.VisualStudio.Core.Runtime
{
    public sealed class DefaultSidecarHealthProbe : ISidecarHealthProbe
    {
        private readonly HttpClient _httpClient;

        public DefaultSidecarHealthProbe(HttpClient httpClient) =>
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task CheckReadyAsync(Uri address, CancellationToken cancellationToken)
        {
            using (var response = await _httpClient.GetAsync(
                new Uri(address, "health/ready"),
                cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new SidecarStartupException(
                        $"Cliq health check returned HTTP {(int)response.StatusCode}.");
                }
            }
        }
    }
}
