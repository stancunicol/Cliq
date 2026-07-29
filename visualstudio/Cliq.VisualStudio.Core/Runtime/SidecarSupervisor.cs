using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cliq.VisualStudio.Core.Runtime
{
    public sealed class SidecarSupervisor : IDisposable
    {
        private const string ReadyPrefix = "CLIQ_READY ";
        private readonly SidecarOptions _options;
        private readonly ISidecarProcessFactory _processFactory;
        private readonly ISidecarHealthProbe _healthProbe;
        private readonly Action<string> _diagnostic;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly object _stateSync = new object();
        private ISidecarProcess? _process;
        private SidecarConnection? _connection;
        private bool _disposed;

        public SidecarSupervisor(
            SidecarOptions options,
            ISidecarProcessFactory processFactory,
            ISidecarHealthProbe healthProbe,
            Action<string>? diagnostic = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
            _healthProbe = healthProbe ?? throw new ArgumentNullException(nameof(healthProbe));
            _diagnostic = diagnostic ?? (_ => { });
            if (_options.StartupTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "The sidecar startup timeout must be positive.");
            }
        }

        public async Task<SidecarConnection> EnsureStartedAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                ISidecarProcess? currentProcess;
                SidecarConnection? currentConnection;
                lock (_stateSync)
                {
                    currentProcess = _process;
                    currentConnection = _connection;
                }

                if (currentProcess != null && currentConnection != null && !currentProcess.HasExited)
                {
                    return currentConnection;
                }

                if (currentProcess != null)
                {
                    ClearState(currentProcess);
                    currentProcess.Dispose();
                }

                return await StartAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ISidecarProcess? process;
                lock (_stateSync)
                {
                    process = _process;
                    _process = null;
                    _connection = null;
                }

                if (process == null)
                {
                    return;
                }

                try
                {
                    if (!process.HasExited)
                    {
                        process.KillTree();
                    }

                    var completed = await Task.WhenAny(
                        process.ExitTask,
                        Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)).ConfigureAwait(false);
                    if (completed != process.ExitTask)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Report("Cliq sidecar did not confirm shutdown within five seconds.", null);
                    }
                }
                finally
                {
                    process.Dispose();
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            finally
            {
                _gate.Dispose();
            }
        }

        private async Task<SidecarConnection> StartAsync(CancellationToken cancellationToken)
        {
            var token = CreateToken();
            ISidecarProcess? process = null;
            try
            {
                var startInfo = CreateStartInfo(token);
                Report("Starting the Cliq sidecar.", token);
                process = _processFactory.Start(startInfo);
                _ = DrainErrorsAsync(process, token);
                var address = await WaitForReadyAsync(process, token, cancellationToken).ConfigureAwait(false);
                await CheckHealthWithinStartupTimeoutAsync(address, cancellationToken).ConfigureAwait(false);
                if (process.HasExited)
                {
                    throw new SidecarStartupException("The Cliq sidecar exited during startup.");
                }

                var connection = new SidecarConnection(address, token, process.Id);
                lock (_stateSync)
                {
                    _process = process;
                    _connection = connection;
                }

                _ = ObserveExitAsync(process, token);
                Report($"Cliq sidecar is ready at {address}.", token);
                return connection;
            }
            catch (OperationCanceledException)
            {
                TerminateFailedStart(process);
                throw;
            }
            catch (SidecarStartupException)
            {
                TerminateFailedStart(process);
                throw;
            }
            catch (Exception exception)
            {
                TerminateFailedStart(process);
                throw new SidecarStartupException("The Cliq sidecar failed to start.", exception);
            }
        }

        private async Task<Uri> WaitForReadyAsync(
            ISidecarProcess process,
            string token,
            CancellationToken cancellationToken)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(_options.StartupTimeout);
                while (true)
                {
                    Task<string?> outputTask = process.ReadOutputLineAsync();
                    var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token);
                    var completed = await Task.WhenAny(
                        outputTask,
                        process.ExitTask,
                        cancellationTask).ConfigureAwait(false);

                    if (completed == process.ExitTask)
                    {
                        throw new SidecarStartupException(
                            $"The Cliq sidecar exited before becoming ready (exit code {await process.ExitTask.ConfigureAwait(false)}).");
                    }

                    if (completed == cancellationTask)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new SidecarStartupException(
                            $"The Cliq sidecar did not become ready within {_options.StartupTimeout.TotalSeconds:0} seconds.",
                            new TimeoutException());
                    }

                    var line = await outputTask.ConfigureAwait(false);
                    if (line == null)
                    {
                        throw new SidecarStartupException(
                            "The Cliq sidecar closed stdout before publishing its ready message.");
                    }

                    if (!line.StartsWith(ReadyPrefix, StringComparison.Ordinal))
                    {
                        Report(line, token);
                        continue;
                    }

                    return ParseReadyAddress(line.Substring(ReadyPrefix.Length));
                }
            }
        }

        private async Task CheckHealthWithinStartupTimeoutAsync(
            Uri address,
            CancellationToken cancellationToken)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(_options.StartupTimeout);
                try
                {
                    await _healthProbe.CheckReadyAsync(address, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new SidecarStartupException(
                        "The Cliq sidecar health check timed out.",
                        new TimeoutException());
                }
            }
        }

        private async Task ObserveExitAsync(ISidecarProcess process, string token)
        {
            var exitCode = await process.ExitTask.ConfigureAwait(false);
            var wasCurrent = ClearState(process);
            if (wasCurrent)
            {
                Report(
                    $"Cliq sidecar exited unexpectedly with code {exitCode}; it will restart on the next command.",
                    token);
                process.Dispose();
            }
        }

        private async Task DrainErrorsAsync(ISidecarProcess process, string token)
        {
            try
            {
                while (true)
                {
                    var line = await process.ReadErrorLineAsync().ConfigureAwait(false);
                    if (line == null)
                    {
                        return;
                    }

                    Report(line, token);
                }
            }
            catch (Exception exception)
            {
                if (!process.HasExited)
                {
                    Report($"Unable to read Cliq sidecar diagnostics: {exception.Message}", token);
                }
            }
        }

        private SidecarProcessStartInfo CreateStartInfo(string token)
        {
            string fileName;
            string arguments;
            if (!string.IsNullOrWhiteSpace(_options.PackagedExecutablePath)
                && File.Exists(_options.PackagedExecutablePath))
            {
                fileName = Path.GetFullPath(_options.PackagedExecutablePath);
                arguments = "--urls http://127.0.0.1:0";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_options.BackendProjectPath)
                    || !File.Exists(_options.BackendProjectPath))
                {
                    throw new SidecarStartupException(
                        "The packaged Cliq sidecar and development backend project were not found.");
                }

                fileName = "dotnet";
                arguments =
                    $"run --project {Quote(Path.GetFullPath(_options.BackendProjectPath))} " +
                    "--no-launch-profile -- --urls http://127.0.0.1:0";
            }

            var environment = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["CLIQ_API_TOKEN"] = token,
                ["CLIQ_HOST_KIND"] = "visualstudio",
            };
            foreach (var pair in _options.AdditionalEnvironment)
            {
                environment[pair.Key] = pair.Value;
            }
            if (!string.IsNullOrWhiteSpace(_options.NodeExecutablePath))
            {
                environment["CLIQ_NODE_PATH"] = Path.GetFullPath(_options.NodeExecutablePath);
            }

            if (!string.IsNullOrWhiteSpace(_options.ScannerScriptPath))
            {
                environment["CLIQ_PUPPETEER_SCANNER_PATH"] =
                    Path.GetFullPath(_options.ScannerScriptPath);
            }

            return new SidecarProcessStartInfo(
                fileName,
                arguments,
                Path.GetFullPath(_options.WorkingDirectory),
                environment);
        }

        internal static Uri ParseReadyAddress(string json)
        {
            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    if (!document.RootElement.TryGetProperty("address", out var addressProperty)
                        || addressProperty.ValueKind != JsonValueKind.String
                        || !Uri.TryCreate(addressProperty.GetString(), UriKind.Absolute, out var address)
                        || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
                        || !address.IsLoopback)
                    {
                        throw new SidecarStartupException(
                            "The Cliq sidecar published an invalid or non-loopback address.");
                    }

                    return new Uri(address.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
                }
            }
            catch (JsonException exception)
            {
                throw new SidecarStartupException(
                    "The Cliq sidecar published malformed ready metadata.",
                    exception);
            }
        }

        private static string CreateToken()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

        private bool ClearState(ISidecarProcess process)
        {
            lock (_stateSync)
            {
                if (!ReferenceEquals(_process, process))
                {
                    return false;
                }

                _process = null;
                _connection = null;
                return true;
            }
        }

        private static void TerminateFailedStart(ISidecarProcess? process)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.KillTree();
                }
            }
            finally
            {
                process.Dispose();
            }
        }

        private void Report(string message, string? token) =>
            _diagnostic(SidecarDiagnosticSanitizer.Sanitize(
                message,
                token == null ? null : new[] { token }));

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SidecarSupervisor));
            }
        }
    }
}
