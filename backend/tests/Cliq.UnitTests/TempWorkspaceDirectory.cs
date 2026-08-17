namespace Cliq.UnitTests;

/// <summary>
/// A uniquely-named temp directory that's deleted on Dispose, retrying past a transient
/// IOException (a Windows file lock can outlive the operation that caused it by a few ms) rather
/// than letting cleanup flake. Mirrors Cliq.IntegrationTests.TemporaryWorkspace - kept as an
/// independent copy rather than a cross-project reference so this test project doesn't gain a
/// dependency on the integration test project just for this.
/// </summary>
internal sealed class TempWorkspaceDirectory : IDisposable
{
    internal TempWorkspaceDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "cliq-unit-tests", Guid.NewGuid().ToString("N"));
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
