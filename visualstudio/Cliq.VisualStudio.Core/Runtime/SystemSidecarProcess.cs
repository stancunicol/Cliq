using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

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

            try
            {
                return new SystemSidecarProcess(process);
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }

                process.Dispose();
                throw;
            }
        }
    }

    internal sealed class SystemSidecarProcess : ISidecarProcess
    {
        private readonly Process _process;
        private readonly SafeJobHandle _job;
        private readonly TaskCompletionSource<int> _exit =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _disposed;

        public SystemSidecarProcess(Process process)
        {
            _process = process;
            _job = ProcessJob.Create(process);
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
            _job.Dispose();
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

    internal static class ProcessJob
    {
        private const uint JobObjectLimitKillOnJobClose = 0x00002000;
        private const int ExtendedLimitInformationClass = 9;

        public static SafeJobHandle Create(Process process)
        {
            var job = CreateJobObject(IntPtr.Zero, null);
            if (job.IsInvalid)
            {
                throw CreateException("create a sidecar job object");
            }

            try
            {
                var information = new JobObjectExtendedLimitInformation
                {
                    BasicLimitInformation = new JobObjectBasicLimitInformation
                    {
                        LimitFlags = JobObjectLimitKillOnJobClose,
                    },
                };
                var length = Marshal.SizeOf(typeof(JobObjectExtendedLimitInformation));
                if (!SetInformationJobObject(
                        job,
                        ExtendedLimitInformationClass,
                        ref information,
                        (uint)length))
                {
                    throw CreateException("configure sidecar crash cleanup");
                }

                if (!AssignProcessToJobObject(job, process.Handle))
                {
                    throw CreateException("assign the sidecar to its crash-cleanup job");
                }

                return job;
            }
            catch
            {
                job.Dispose();
                throw;
            }
        }

        private static SidecarStartupException CreateException(string operation) =>
            new SidecarStartupException(
                $"Windows could not {operation}.",
                new Win32Exception(Marshal.GetLastWin32Error()));

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeJobHandle CreateJobObject(IntPtr securityAttributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            SafeJobHandle job,
            int informationClass,
            ref JobObjectExtendedLimitInformation information,
            uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(SafeJobHandle job, IntPtr process);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }

    internal sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeJobHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle() => CloseHandle(handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
