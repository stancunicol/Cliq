using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceRepositoryTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public async Task OpenAsync_InitializesVersionedWorkspace()
    {
        var repository = new WorkspaceRepository();

        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Project.SchemaVersion, Is.EqualTo(ProjectManifest.CurrentSchemaVersion));
            Assert.That(File.Exists(Path.Combine(_root, ".cliq", "project.json")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_root, ".cliq", ".gitignore")), Does.Contain("secrets.local.json"));
        });
    }

    [Test]
    public async Task SaveTestAsync_RoundTripsTestSpecification()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            ["critical"],
            [new TestStep("open", StepAction.Navigate, "Open shop", true, Url: "https://example.com")],
            DateTimeOffset.UtcNow);

        await repository.SaveTestAsync(test, CancellationToken.None);
        var loaded = await repository.GetTestAsync(test.Id, CancellationToken.None);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Steps.Single().Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public async Task GetContainedPath_RejectsTraversal()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        Assert.Throws<WorkspaceValidationException>(() => repository.GetContainedPath("../outside"));
    }

    [Test]
    public void OpenAsync_RejectsMissingDirectory()
    {
        var repository = new WorkspaceRepository();

        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await repository.OpenAsync(Path.Combine(_root, "missing"), CancellationToken.None));
    }
}
