using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Cliq.VisualStudio.Core.Runtime
{
    public sealed class SystemSidecarProcessFactory : ISidecarProcessFactory
    {
        public ISidecarProcess Start(SidecarProcessStartInfo startInfo)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = startInfo.FileName,
                Arguments = startInfo.Arguments,
                WorkingDirectory = startInfo.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (KeyValuePair<string, string> pair in startInfo.Environment)
            {
                processStartInfo.EnvironmentVariables[pair.Key] = pair.Value;
            }

            var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
            if (!process.Start())
            {
                process.Dispose();
                throw new SidecarStartupException("The Cliq sidecar process could not be started.");
            }

            return new SystemSidecarProcess(process);
        }
    }

    internal sealed class SystemSidecarProcess : ISidecarProcess
    {
        private readonly Process _process;
        private readonly TaskCompletionSource<int> _exit =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _disposed;

        public SystemSidecarProcess(Process process)
        {
            _process = process;
            _process.Exited += OnExited;
            if (_process.HasExited)
            {
                OnExited(this, EventArgs.Empty);
            }
        }

        public int Id => _process.Id;
        public bool HasExited => _process.HasExited;
        public Task<int> ExitTask => _exit.Task;
        public Task<string?> ReadOutputLineAsync() => _process.StandardOutput.ReadLineAsync();
        public Task<string?> ReadErrorLineAsync() => _process.StandardError.ReadLineAsync();

        public void KillTree()
        {
            if (_process.HasExited)
            {
                return;
            }

            try
            {
                var taskkillPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "taskkill.exe");
                using (var killer = Process.Start(new ProcessStartInfo
                {
                    FileName = taskkillPath,
                    Arguments = $"/PID {_process.Id} /T /F",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }))
                {
                    killer?.WaitForExit(5000);
                }
            }
            catch
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _process.Exited -= OnExited;
            _process.Dispose();
        }

        private void OnExited(object? sender, EventArgs args)
        {
            try
            {
                _exit.TrySetResult(_process.ExitCode);
            }
            catch (InvalidOperationException)
            {
                _exit.TrySetResult(-1);
            }
        }
    }
}
