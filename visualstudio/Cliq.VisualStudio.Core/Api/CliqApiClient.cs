using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Api
{
    public sealed class CliqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _baseAddress;
        private readonly string _token;

        public CliqApiClient(HttpClient httpClient, Uri baseAddress, string token)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            _token = !string.IsNullOrWhiteSpace(token)
                ? token
                : throw new ArgumentException("The API token is required.", nameof(token));
        }

        public Task<WorkspaceSnapshot> OpenWorkspaceAsync(string rootPath, CancellationToken cancellationToken) =>
            SendJsonAsync<WorkspaceSnapshot>(
                HttpMethod.Post,
                "api/v1/workspaces/open",
                new OpenWorkspaceRequest(rootPath),
                cancellationToken);

        public Task CloseWorkspaceAsync(CancellationToken cancellationToken) =>
            SendNoContentAsync(HttpMethod.Post, "api/v1/workspaces/close", null, cancellationToken);

        public Task<WorkspaceSnapshot> GetWorkspaceAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<WorkspaceSnapshot>(HttpMethod.Get, "api/v1/workspace", null, cancellationToken);

        public Task<ProjectManifest> GetProjectAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<ProjectManifest>(HttpMethod.Get, "api/v1/project", null, cancellationToken);

        public Task<ProjectManifest> SaveProjectAsync(ProjectManifest project, CancellationToken cancellationToken) =>
            SendJsonAsync<ProjectManifest>(HttpMethod.Put, "api/v1/project", project, cancellationToken);

        public Task<CliqSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<CliqSettings>(HttpMethod.Get, "api/v1/settings", null, cancellationToken);

        public Task<CliqSettings> SaveSettingsAsync(CliqSettings settings, CancellationToken cancellationToken) =>
            SendJsonAsync<CliqSettings>(HttpMethod.Put, "api/v1/settings", settings, cancellationToken);

        public Task<IReadOnlyList<PageDefinition>> ListPagesAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<IReadOnlyList<PageDefinition>>(HttpMethod.Get, "api/v1/pages", null, cancellationToken);

        public Task<PageDefinition> GetPageAsync(string id, CancellationToken cancellationToken) =>
            SendJsonAsync<PageDefinition>(HttpMethod.Get, $"api/v1/pages/{Escape(id)}", null, cancellationToken);

        public Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken) =>
            SendJsonAsync<PageDefinition>(
                HttpMethod.Put,
                $"api/v1/pages/{Escape(page.Id)}",
                page,
                cancellationToken);

        public Task DeletePageAsync(string id, CancellationToken cancellationToken) =>
            SendNoContentAsync(HttpMethod.Delete, $"api/v1/pages/{Escape(id)}", null, cancellationToken);

        public Task<GeneratedPathResponse> GetGeneratedPageObjectPathAsync(string pageId, CancellationToken cancellationToken) =>
            SendJsonAsync<GeneratedPathResponse>(
                HttpMethod.Get, $"api/v1/pages/{Escape(pageId)}/generated-path", null, cancellationToken);

        public Task<ScanPageResponse> ScanPageAsync(ScanPageRequest request, CancellationToken cancellationToken) =>
            SendJsonAsync<ScanPageResponse>(HttpMethod.Post, "api/v1/pages/scan", request, cancellationToken);

        public Task<StagedPageScanResult> ScanPageReviewAsync(ScanPageRequest request, CancellationToken cancellationToken) =>
            SendJsonAsync<StagedPageScanResult>(HttpMethod.Post, "api/v1/pages/scan-reviews", request, cancellationToken);

        public Task<IReadOnlyList<ScanReview>> ListScanReviewsAsync(
            string? pageId = null,
            ScanReviewStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(pageId))
            {
                query.Add($"pageId={Escape(pageId!)}");
            }

            if (status.HasValue)
            {
                query.Add($"status={Escape(status.Value.ToString().ToLowerInvariant())}");
            }

            var path = query.Count > 0 ? $"api/v1/scan-reviews?{string.Join("&", query)}" : "api/v1/scan-reviews";
            return SendJsonAsync<IReadOnlyList<ScanReview>>(HttpMethod.Get, path, null, cancellationToken);
        }

        public Task<ScanReview> GetScanReviewAsync(string reviewId, CancellationToken cancellationToken) =>
            SendJsonAsync<ScanReview>(HttpMethod.Get, $"api/v1/scan-reviews/{Escape(reviewId)}", null, cancellationToken);

        public Task<ScanReview> UpdateScanReviewDecisionsAsync(
            string reviewId,
            UpdateScanReviewDecisionsRequest request,
            CancellationToken cancellationToken) =>
            SendJsonAsync<ScanReview>(
                HttpMethod.Put,
                $"api/v1/scan-reviews/{Escape(reviewId)}/decisions",
                request,
                cancellationToken);

        public Task<ScanReviewApplyResult> ApplyScanReviewAsync(string reviewId, CancellationToken cancellationToken) =>
            SendJsonAsync<ScanReviewApplyResult>(
                HttpMethod.Post,
                $"api/v1/scan-reviews/{Escape(reviewId)}/apply",
                null,
                cancellationToken);

        public Task<ScanReview> DiscardScanReviewAsync(string reviewId, CancellationToken cancellationToken) =>
            SendJsonAsync<ScanReview>(
                HttpMethod.Post,
                $"api/v1/scan-reviews/{Escape(reviewId)}/discard",
                null,
                cancellationToken);


        public Task LocateElementAsync(
            string pageId,
            string elementId,
            StartRunRequest request,
            CancellationToken cancellationToken) =>
            SendNoContentAsync(
                HttpMethod.Post,
                $"api/v1/pages/{Escape(pageId)}/elements/{Escape(elementId)}/locate",
                request,
                cancellationToken);

        public Task LocateElementFromSourceAsync(
            LocateFromSourceRequest request,
            CancellationToken cancellationToken) =>
            SendNoContentAsync(
                HttpMethod.Post,
                "api/v1/pages/locate-from-source",
                request,
                cancellationToken);

        public Task<IReadOnlyList<TestCase>> ListTestsAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<IReadOnlyList<TestCase>>(HttpMethod.Get, "api/v1/tests", null, cancellationToken);

        public Task<TestCase> GetTestAsync(string id, CancellationToken cancellationToken) =>
            SendJsonAsync<TestCase>(HttpMethod.Get, $"api/v1/tests/{Escape(id)}", null, cancellationToken);

        public Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken) =>
            SendJsonAsync<TestCase>(
                HttpMethod.Put,
                $"api/v1/tests/{Escape(test.Id)}",
                test,
                cancellationToken);

        public Task DeleteTestAsync(string id, CancellationToken cancellationToken) =>
            SendNoContentAsync(HttpMethod.Delete, $"api/v1/tests/{Escape(id)}", null, cancellationToken);

        public Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken) =>
            SendJsonAsync<IReadOnlyDictionary<string, string>>(
                HttpMethod.Get,
                "api/v1/secrets",
                null,
                cancellationToken);

        public Task SaveSecretsAsync(
            IReadOnlyDictionary<string, string> values,
            CancellationToken cancellationToken) =>
            SendNoContentAsync(HttpMethod.Put, "api/v1/secrets", values, cancellationToken);

        public Task DeleteSecretAsync(string key, CancellationToken cancellationToken) =>
            SendNoContentAsync(
                HttpMethod.Delete,
                $"api/v1/secrets/{Escape(key)}",
                null,
                cancellationToken);

        public Task<GenerationResult> GenerateAsync(
            GenerationRequest request,
            CancellationToken cancellationToken) =>
            SendJsonAsync<GenerationResult>(
                HttpMethod.Post,
                "api/v1/generation",
                request,
                cancellationToken);

        public Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(
            CancellationToken cancellationToken) =>
            SendJsonAsync<IReadOnlyList<PendingCustomMethodEntry>>(
                HttpMethod.Get, "api/v1/pending-custom-methods", null, cancellationToken);

        public Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken) =>
            SendNoContentAsync(
                HttpMethod.Delete, $"api/v1/pending-custom-methods/{Escape(id)}", null, cancellationToken);

        public Task<TestRun> StartRunAsync(
            string testId,
            StartRunRequest request,
            CancellationToken cancellationToken) =>
            SendJsonAsync<TestRun>(
                HttpMethod.Post,
                $"api/v1/tests/{Escape(testId)}/runs",
                request,
                cancellationToken);

        public Task<TestRun> GetRunAsync(string runId, CancellationToken cancellationToken) =>
            SendJsonAsync<TestRun>(HttpMethod.Get, $"api/v1/runs/{Escape(runId)}", null, cancellationToken);

        public Task<TestRun> SaveRunAsync(TestRun run, CancellationToken cancellationToken) =>
            SendJsonAsync<TestRun>(
                HttpMethod.Put,
                $"api/v1/runs/{Escape(run.Id)}",
                run,
                cancellationToken);

        public Task CancelRunAsync(string runId, CancellationToken cancellationToken) =>
            SendNoContentAsync(
                HttpMethod.Post,
                $"api/v1/runs/{Escape(runId)}/cancel",
                null,
                cancellationToken,
                HttpStatusCode.Accepted);

        public async Task<byte[]> GetScreenshotAsync(
            string runId,
            string fileName,
            CancellationToken cancellationToken)
        {
            using (var request = CreateRequest(
                HttpMethod.Get,
                $"api/v1/runs/{Escape(runId)}/screenshots/{Escape(fileName)}",
                null))
            using (var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        public async Task StreamRunEventsAsync(
            string runId,
            Func<RunStreamEvent, Task> onEvent,
            CancellationToken cancellationToken)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            using (var request = CreateRequest(
                HttpMethod.Get,
                $"api/v1/runs/{Escape(runId)}/events",
                null))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                using (var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                    {
                        string? eventName = null;
                        var data = new StringBuilder();
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var readTask = reader.ReadLineAsync();
                            var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                            var completedTask = await Task.WhenAny(
                                readTask,
                                cancellationTask).ConfigureAwait(false);
                            if (completedTask == cancellationTask)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }

                            var line = await readTask.ConfigureAwait(false);
                            if (line == null)
                            {
                                break;
                            }

                            if (line.Length == 0)
                            {
                                if (eventName != null)
                                {
                                    var parsed = ParseRunEvent(eventName, data.ToString());
                                    await onEvent(parsed).ConfigureAwait(false);
                                    if (parsed.IsTerminal)
                                    {
                                        return;
                                    }
                                }

                                eventName = null;
                                data.Clear();
                            }
                            else if (line.StartsWith("event:", StringComparison.Ordinal))
                            {
                                eventName = line.Substring("event:".Length).Trim();
                            }
                            else if (line.StartsWith("data:", StringComparison.Ordinal))
                            {
                                if (data.Length > 0)
                                {
                                    data.Append('\n');
                                }

                                data.Append(line.Substring("data:".Length).TrimStart());
                            }
                        }

                        if (eventName != null)
                        {
                            var parsed = ParseRunEvent(eventName, data.ToString());
                            await onEvent(parsed).ConfigureAwait(false);
                            if (parsed.IsTerminal)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            throw new RunEventStreamDisconnectedException(runId);
        }

        internal static RunStreamEvent ParseRunEvent(string eventName, string data)
        {
            if (eventName == "step")
            {
                return RunStreamEvent.StepCompleted(DeserializeRequired<StepResult>(data));
            }

            if (eventName == "done")
            {
                return RunStreamEvent.Done(DeserializeRequired<TestRun>(data));
            }

            throw new InvalidDataException($"Unknown Cliq run event '{eventName}'.");
        }

        private async Task<T> SendJsonAsync<T>(
            HttpMethod method,
            string relativePath,
            object? body,
            CancellationToken cancellationToken)
        {
            using (var request = CreateRequest(method, relativePath, body))
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return DeserializeRequired<T>(json);
            }
        }

        private async Task SendNoContentAsync(
            HttpMethod method,
            string relativePath,
            object? body,
            CancellationToken cancellationToken,
            HttpStatusCode? alternateSuccess = null)
        {
            using (var request = CreateRequest(method, relativePath, body))
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode && response.StatusCode != alternateSuccess)
                {
                    await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, object? body)
        {
            var request = new HttpRequestMessage(method, new Uri(_baseAddress, relativePath));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, body.GetType(), CliqJson.Options),
                    Encoding.UTF8,
                    "application/json");
            }

            return request;
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task ThrowApiExceptionAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var json = response.Content == null
                ? string.Empty
                : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var problem = ParseProblem(json, response.StatusCode);
            if (string.Equals(problem.Code, "workspace_in_use", StringComparison.Ordinal))
            {
                throw new WorkspaceInUseApiException(response.StatusCode, problem);
            }

            if (problem.Code != null && problem.Code.StartsWith("scan_review_", StringComparison.Ordinal))
            {
                IReadOnlyList<ScanReviewReference>? references = null;
                if (problem.Extensions.TryGetValue("references", out var referencesElement) &&
                    referencesElement.ValueKind == JsonValueKind.Array)
                {
                    references = referencesElement.Deserialize<IReadOnlyList<ScanReviewReference>>(CliqJson.Options);
                }

                throw new ScanReviewConflictApiException(response.StatusCode, problem, references);
            }

            throw new CliqApiException(response.StatusCode, problem);
        }

        private static CliqProblemDetails ParseProblem(string json, HttpStatusCode statusCode)
        {
            var problem = new CliqProblemDetails { Status = (int)statusCode };
            if (string.IsNullOrWhiteSpace(json))
            {
                return problem;
            }

            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        switch (property.Name)
                        {
                            case "type":
                                problem.Type = property.Value.GetString();
                                break;
                            case "title":
                                problem.Title = property.Value.GetString();
                                break;
                            case "status":
                                problem.Status = property.Value.GetInt32();
                                break;
                            case "detail":
                                problem.Detail = property.Value.GetString();
                                break;
                            case "instance":
                                problem.Instance = property.Value.GetString();
                                break;
                            case "code":
                                problem.Code = property.Value.GetString();
                                break;
                            case "owner":
                                problem.Owner = property.Value.Deserialize<WorkspaceLeaseOwner>(CliqJson.Options);
                                break;
                            default:
                                problem.Extensions[property.Name] = property.Value.Clone();
                                if (string.Equals(property.Name, "code", StringComparison.OrdinalIgnoreCase) && problem.Code == null)
                                {
                                    problem.Code = property.Value.GetString();
                                }
                                break;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                problem.Detail = $"Cliq returned HTTP {(int)statusCode} with a non-JSON error body.";
            }

            return problem;
        }

        private static T DeserializeRequired<T>(string json)
        {
            var value = JsonSerializer.Deserialize<T>(json, CliqJson.Options);
            if (value == null)
            {
                throw new InvalidDataException($"Cliq returned an empty {typeof(T).Name} payload.");
            }

            return value;
        }

        private static string Escape(string value) => Uri.EscapeDataString(value);
    }
}
