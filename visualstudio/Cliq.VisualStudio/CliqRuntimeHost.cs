using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Runtime;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

internal sealed class CliqOutputLogger
{
    private static readonly Guid PaneGuid = new("94E7846D-40E4-4E44-B369-DC56E6FBBAF7");
    private readonly AsyncPackage _package;
    private IVsOutputWindowPane? _pane;

    public CliqOutputLogger(AsyncPackage package) =>
        _package = package ?? throw new ArgumentNullException(nameof(package));

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (await _package.GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow output)
        {
            var paneGuid = PaneGuid;
            ErrorHandler.ThrowOnFailure(
                output.CreatePane(ref paneGuid, "Cliq", fInitVisible: 1, fClearWithSolution: 0));
            ErrorHandler.ThrowOnFailure(output.GetPane(ref paneGuid, out _pane));
        }
    }

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "Diagnostics are intentionally queued to the UI thread and observed.")]
    public void Write(string message)
    {
        var sanitized = SidecarDiagnosticSanitizer.Sanitize(message);
        _package.JoinableTaskFactory
            .RunAsync(
                async () =>
                {
                    await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _pane?.OutputStringThreadSafe($"[{DateTime.Now:HH:mm:ss}] {sanitized}{Environment.NewLine}");
                })
            .FileAndForget("Cliq/Output");
    }
}

internal sealed class CliqSidecarRuntime : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SidecarSupervisor _supervisor;
    private bool _disposed;

    public CliqSidecarRuntime(Action<string> diagnostic)
    {
        _httpClient = new HttpClient();
        var options = CreateOptions();
        _supervisor = new SidecarSupervisor(
            options,
            new SystemSidecarProcessFactory(),
            new DefaultSidecarHealthProbe(_httpClient),
            diagnostic);
    }

    public async Task<CliqApiClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CliqSidecarRuntime));
        }

        var connection = await _supervisor.EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
        return connection.CreateApiClient(_httpClient);
    }

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await _supervisor.StopAsync(cancellationToken).ConfigureAwait(false);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _supervisor.Dispose();
        _httpClient.Dispose();
    }

    private static SidecarOptions CreateOptions()
    {
        var installDirectory = Path.GetDirectoryName(typeof(CliqPackage).Assembly.Location)
            ?? Environment.CurrentDirectory;
        var payloadDirectory = Path.Combine(installDirectory, "Payload");
        var runtimeDirectory = Path.Combine(payloadDirectory, "runtime");
        var backendExecutable = Path.Combine(runtimeDirectory, "api", "Cliq.Api.exe");
        var nodeExecutable = Path.Combine(runtimeDirectory, "node", "node.exe");
        var scannerScript = Path.Combine(runtimeDirectory, "scanner", "scan.js");
        var repositoryRoot = FindRepositoryRoot(installDirectory)
            ?? FindRepositoryRoot(Environment.CurrentDirectory);
        var backendProject = repositoryRoot is null
            ? null
            : Path.Combine(repositoryRoot, "backend", "src", "Cliq.Api", "Cliq.Api.csproj");
        var workingDirectory = File.Exists(backendExecutable)
            ? Path.GetDirectoryName(backendExecutable)!
            : backendProject is not null && File.Exists(backendProject)
                ? Path.GetDirectoryName(backendProject)!
                : installDirectory;

        return new SidecarOptions
        {
            PackagedExecutablePath = backendExecutable,
            BackendProjectPath = backendProject,
            WorkingDirectory = workingDirectory,
            NodeExecutablePath = File.Exists(nodeExecutable) ? nodeExecutable : null,
            ScannerScriptPath = File.Exists(scannerScript) ? scannerScript : null,
        };
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cliq.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

internal sealed class VisualStudioCliqSession : ICliqApiSession, IDisposable
{
    private readonly AsyncPackage _package;
    private readonly VisualStudioContextService _context;
    private readonly CliqSidecarRuntime _runtime;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _workspaceRoot;
    private bool _disposed;

    public VisualStudioCliqSession(
        AsyncPackage package,
        VisualStudioContextService context,
        CliqSidecarRuntime runtime)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string? WorkspaceRoot => _workspaceRoot;

    public async Task<CliqApiClient> GetClientAsync(CancellationToken cancellationToken)
    {
        var root = _workspaceRoot ?? await ResolveWorkspaceAsync(cancellationToken);
        return await OpenAsync(root, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ResolveCurrentWorkspaceAsync(CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var snapshot = _context.Capture();
        return WorkspacePathResolver.ResolveDefault(snapshot, _context.GetOverride(snapshot));
    }

    public async Task<CliqApiClient> OpenAsync(string rootPath, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VisualStudioCliqSession));
        }

        var normalized = Path.GetFullPath(rootPath);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var client = await _runtime.GetClientAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(_workspaceRoot, normalized, StringComparison.OrdinalIgnoreCase))
            {
                await client.OpenWorkspaceAsync(normalized, cancellationToken).ConfigureAwait(false);
                _workspaceRoot = normalized;
            }

            return client;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_workspaceRoot is null)
            {
                return;
            }

            var client = await _runtime.GetClientAsync(cancellationToken).ConfigureAwait(false);
            await client.CloseWorkspaceAsync(cancellationToken).ConfigureAwait(false);
            _workspaceRoot = null;
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
        _gate.Dispose();
    }

    private async Task<string> ResolveWorkspaceAsync(CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var snapshot = _context.Capture();
        var root = WorkspacePathResolver.ResolveDefault(snapshot, _context.GetOverride(snapshot));
        return root ?? throw new InvalidOperationException(
            "Open a solution or folder, or bind a Cliq workspace from Solution Explorer.");
    }
}
