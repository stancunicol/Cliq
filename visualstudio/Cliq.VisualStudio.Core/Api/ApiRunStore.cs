using System;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Runs;

namespace Cliq.VisualStudio.Core.Api
{
    public sealed class ApiRunStore : IRunStore
    {
        private readonly ICliqApiSession _session;
        private readonly SynchronizationContext? _callbackContext;

        public ApiRunStore(
            ICliqApiSession session,
            SynchronizationContext? callbackContext = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _callbackContext = callbackContext;
        }

        public async Task<TestRun> StartAsync(
            string testId,
            BrowserKind browser,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .StartRunAsync(testId, new StartRunRequest(browser), cancellationToken)
                .ConfigureAwait(false);

        public async Task StreamAsync(
            string runId,
            Func<RunStreamEvent, Task> onEvent,
            CancellationToken cancellationToken)
        {
            var client = await _session.GetClientAsync(cancellationToken).ConfigureAwait(false);
            await client.StreamRunEventsAsync(
                runId,
                runEvent => InvokeCallbackAsync(runEvent, onEvent),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<TestRun> GetAsync(string runId, CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .GetRunAsync(runId, cancellationToken).ConfigureAwait(false);

        public async Task CancelAsync(string runId, CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .CancelRunAsync(runId, cancellationToken).ConfigureAwait(false);

        public async Task<byte[]> GetScreenshotAsync(
            string runId,
            string fileName,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .GetScreenshotAsync(runId, fileName, cancellationToken).ConfigureAwait(false);

        private Task InvokeCallbackAsync(
            RunStreamEvent runEvent,
            Func<RunStreamEvent, Task> callback)
        {
            if (_callbackContext is null ||
                ReferenceEquals(SynchronizationContext.Current, _callbackContext))
            {
                return callback(runEvent);
            }

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _callbackContext.Post(
                async _ =>
                {
                    try
                    {
                        await callback(runEvent);
                        completion.TrySetResult(true);
                    }
                    catch (Exception exception)
                    {
                        completion.TrySetException(exception);
                    }
                },
                null);
            return completion.Task;
        }
    }
}
