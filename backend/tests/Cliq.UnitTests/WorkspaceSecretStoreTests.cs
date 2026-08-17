using Cliq.Application;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceSecretStoreTests
{
    private TempWorkspaceDirectory _workspace = null!;
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _workspace = new TempWorkspaceDirectory();
        _root = _workspace.RootPath;
    }

    [TearDown]
    public void TearDown() => _workspace.Dispose();

    [Test]
    public async Task DeleteAsync_RemovesOnlyRequestedSecret()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var store = new WorkspaceSecretStore(repository);
        await store.SaveAsync(
            new Dictionary<string, string>
            {
                ["username"] = "alice",
                ["password"] = "secret",
            },
            CancellationToken.None);

        await store.DeleteAsync("password", CancellationToken.None);

        var values = await store.ReadAsync(CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(values, Does.ContainKey("username"));
            Assert.That(values, Does.Not.ContainKey("password"));
        });
    }

    [Test]
    public async Task DeleteAsync_RejectsBlankKeys()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var store = new WorkspaceSecretStore(repository);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await store.DeleteAsync(" ", CancellationToken.None));
    }
}
