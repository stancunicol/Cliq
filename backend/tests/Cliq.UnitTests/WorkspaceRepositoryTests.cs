using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceRepositoryTests
{
    private static readonly string[] DefaultStrategyOrder = ["testId", "id", "name"];
    private static readonly string[] CustomStrategyOrder = ["id", "testId"];

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
    public async Task OpenAsync_CreatesDefaultSettings()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        var settings = await repository.GetSettingsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_root, ".cliq", "settings.json")), Is.True);
            Assert.That(settings.ElementIdentification.TestIdAttribute, Is.EqualTo("data-testid"));
            Assert.That(settings.ElementIdentification.StrategyOrder, Is.EqualTo(DefaultStrategyOrder));
            Assert.That(settings.TestExecution.DefaultStepTimeoutMs, Is.EqualTo(10_000));
            Assert.That(settings.TestExecution.PageReadyTimeoutMs, Is.EqualTo(15_000));
            Assert.That(settings.Scanning.LoadTimeoutMs, Is.EqualTo(30_000));
            Assert.That(settings.Scanning.NetworkIdleGraceMs, Is.EqualTo(3_000));
            Assert.That(settings.Scanning.CaptureScreenshot, Is.True);
            Assert.That(settings.TestExecution.CaptureScreenshotOnFailure, Is.True);
        });
    }

    [Test]
    public async Task SaveSettingsAsync_RoundTripsCustomIdentificationRules()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = new CliqSettings(
            CliqSettings.CurrentSchemaVersion,
            new ElementIdentificationSettings("data-qa", CustomStrategyOrder),
            new TestExecutionSettings(5_000, 12_000, CaptureScreenshotOnFailure: false),
            new ScanningSettings(20_000, 2_000, CaptureScreenshot: false));

        await repository.SaveSettingsAsync(settings, CancellationToken.None);
        var loaded = await repository.GetSettingsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.ElementIdentification.TestIdAttribute, Is.EqualTo("data-qa"));
            Assert.That(loaded.ElementIdentification.StrategyOrder, Is.EqualTo(CustomStrategyOrder));
            Assert.That(loaded.TestExecution.DefaultStepTimeoutMs, Is.EqualTo(5_000));
            Assert.That(loaded.TestExecution.PageReadyTimeoutMs, Is.EqualTo(12_000));
            Assert.That(loaded.TestExecution.CaptureScreenshotOnFailure, Is.False);
            Assert.That(loaded.Scanning.LoadTimeoutMs, Is.EqualTo(20_000));
            Assert.That(loaded.Scanning.NetworkIdleGraceMs, Is.EqualTo(2_000));
            Assert.That(loaded.Scanning.CaptureScreenshot, Is.False);
        });
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
