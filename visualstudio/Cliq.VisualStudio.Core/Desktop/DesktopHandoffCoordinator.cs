using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cliq.VisualStudio.Core.Desktop
{
    public interface IDesktopWorkspaceLease
    {
        Task CloseAsync(CancellationToken cancellationToken);

        Task<bool> TryAcquireAsync(string workspacePath, CancellationToken cancellationToken);
    }

    public interface IDesktopApplicationLauncher
    {
        void Launch(string executablePath, string workspacePath);
    }

    public sealed class DesktopHandoffCoordinator
    {
        private readonly IDesktopWorkspaceLease _lease;
        private readonly IDesktopApplicationLauncher _launcher;
        private readonly TimeSpan _retryDelay;

        public DesktopHandoffCoordinator(
            IDesktopWorkspaceLease lease,
            IDesktopApplicationLauncher launcher,
            TimeSpan? retryDelay = null)
        {
            _lease = lease ?? throw new ArgumentNullException(nameof(lease));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);
            if (_retryDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryDelay));
            }
        }

        public async Task HandoffAsync(
            string executablePath,
            string workspacePath,
            Action<string>? report,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("The desktop executable path is required.", nameof(executablePath));
            }

            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                throw new ArgumentException("The workspace path is required.", nameof(workspacePath));
            }

            await _lease.CloseAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _launcher.Launch(executablePath, workspacePath);
            }
            catch
            {
                await _lease.TryAcquireAsync(workspacePath, cancellationToken).ConfigureAwait(false);
                throw;
            }

            report?.Invoke("Recording workspace handed to Cliq Desktop.");
            while (true)
            {
                await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                if (await _lease.TryAcquireAsync(workspacePath, cancellationToken).ConfigureAwait(false))
                {
                    report?.Invoke("Cliq Desktop released the workspace; Visual Studio reconnected.");
                    return;
                }

                report?.Invoke("Waiting for Cliq Desktop to release the workspace.");
            }
        }
    }
}
