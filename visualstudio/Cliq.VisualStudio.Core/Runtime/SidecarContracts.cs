using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core.Api;

namespace Cliq.VisualStudio.Core.Runtime
{
    public sealed class SidecarOptions
    {
        public string? PackagedExecutablePath { get; set; }
        public string? BackendProjectPath { get; set; }
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
        public string? NodeExecutablePath { get; set; }
        public string? ScannerScriptPath { get; set; }
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(45);
        public IReadOnlyDictionary<string, string> AdditionalEnvironment { get; set; } =
            new Dictionary<string, string>();
    }

    public sealed class SidecarProcessStartInfo
    {
        public SidecarProcessStartInfo(
            string fileName,
            string arguments,
            string workingDirectory,
            IReadOnlyDictionary<string, string> environment)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            Environment = environment;
        }

        public string FileName { get; }
        public string Arguments { get; }
        public string WorkingDirectory { get; }
        public IReadOnlyDictionary<string, string> Environment { get; }
    }

    public interface ISidecarProcess : IDisposable
    {
        int Id { get; }
        bool HasExited { get; }
        Task<int> ExitTask { get; }
        Task<string?> ReadOutputLineAsync();
        Task<string?> ReadErrorLineAsync();
        void KillTree();
    }

    public interface ISidecarProcessFactory
    {
        ISidecarProcess Start(SidecarProcessStartInfo startInfo);
    }

    public interface ISidecarHealthProbe
    {
        Task CheckReadyAsync(Uri address, CancellationToken cancellationToken);
    }

    public sealed class SidecarConnection
    {
        internal SidecarConnection(Uri address, string token, int processId)
        {
            Address = address;
            Token = token;
            ProcessId = processId;
        }

        public Uri Address { get; }
        public int ProcessId { get; }
        internal string Token { get; }

        public CliqApiClient CreateApiClient(HttpClient httpClient) =>
            new CliqApiClient(httpClient, Address, Token);
    }

    public sealed class SidecarStartupException : Exception
    {
        public SidecarStartupException(string message) : base(message)
        {
        }

        public SidecarStartupException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
