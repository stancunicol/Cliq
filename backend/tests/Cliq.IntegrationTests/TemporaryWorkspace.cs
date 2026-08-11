namespace Cliq.IntegrationTests;

internal sealed class TemporaryWorkspace : IDisposable
{
    internal TemporaryWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "cliq-integration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    internal string RootPath { get; }

    public void Dispose()
    {
        for (var attempt = 0; attempt < 20 && Directory.Exists(RootPath); attempt++)
        {
            try
            {
                Directory.Delete(RootPath, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 19)
            {
                Thread.Sleep(25);
            }
        }
    }
}
