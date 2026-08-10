using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Desktop;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

public sealed class CliqOptionsPage : DialogPage
{
    [Category("Cliq Desktop")]
    [DisplayName("Desktop executable")]
    [Description("Optional path to Cliq.exe. Leave empty to use standard install locations.")]
    [Editor(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(UITypeEditor))]
    public string DesktopExecutablePath { get; set; } = string.Empty;
}

internal static class CliqDesktopDiscovery
{
    public static string? FindExecutable(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var candidates = new[]
        {
            Combine(Environment.SpecialFolder.LocalApplicationData, "Programs", "Cliq", "Cliq.exe"),
            Combine(Environment.SpecialFolder.ProgramFiles, "Cliq", "Cliq.exe"),
            Combine(Environment.SpecialFolder.ProgramFilesX86, "Cliq", "Cliq.exe"),
        };
        foreach (var candidate in candidates)
        {
            if (candidate is not null && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? Combine(Environment.SpecialFolder folder, params string[] children)
    {
        var root = Environment.GetFolderPath(folder);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var path = root;
        foreach (var child in children)
        {
            path = Path.Combine(path, child);
        }

        return path;
    }
}

internal sealed class ProcessDesktopApplicationLauncher : IDesktopApplicationLauncher
{
    public void Launch(string executablePath, string workspacePath)
    {
        using (var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"--workspace \"{workspacePath.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
            }))
        {
            if (process is null)
            {
                throw new InvalidOperationException("Windows did not start Cliq Desktop.");
            }
        }
    }
}

internal sealed class VisualStudioWorkspaceLease : IDesktopWorkspaceLease
{
    private readonly VisualStudioCliqSession _session;

    public VisualStudioWorkspaceLease(VisualStudioCliqSession session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    public Task CloseAsync(CancellationToken cancellationToken) =>
        _session.CloseAsync(cancellationToken);

    public async Task<bool> TryAcquireAsync(
        string workspacePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await _session.OpenAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (WorkspaceInUseApiException exception) when (
            exception.StatusCode == HttpStatusCode.Conflict &&
            string.Equals(
                exception.Problem.Code,
                "workspace_in_use",
                StringComparison.Ordinal))
        {
            return false;
        }
    }
}
