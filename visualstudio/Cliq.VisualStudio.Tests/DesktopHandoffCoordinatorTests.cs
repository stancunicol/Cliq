using Cliq.VisualStudio.Core.Desktop;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class DesktopHandoffCoordinatorTests
{
    [Test]
    public async Task HandoffAsync_ClosesLaunchesAndRetriesUntilLeaseIsAvailable()
    {
        var lease = new RecordingLease(false, false, true);
        var launcher = new RecordingLauncher();
        var messages = new List<string>();
        var coordinator = new DesktopHandoffCoordinator(lease, launcher, TimeSpan.Zero);

        await coordinator.HandoffAsync(
            @"C:\Apps\Cliq\Cliq.exe",
            @"C:\work\shop",
            messages.Add,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(lease.CloseCount, Is.EqualTo(1));
            Assert.That(lease.AcquireCount, Is.EqualTo(3));
            Assert.That(launcher.WorkspacePath, Is.EqualTo(@"C:\work\shop"));
            Assert.That(messages.Last(), Does.Contain("reconnected"));
        });
    }

    [Test]
    public void HandoffAsync_ReacquiresLeaseWhenLaunchFails()
    {
        var lease = new RecordingLease(true);
        var launcher = new RecordingLauncher { Failure = new InvalidOperationException("launch failed") };
        var coordinator = new DesktopHandoffCoordinator(lease, launcher, TimeSpan.Zero);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await coordinator.HandoffAsync(
                @"C:\missing.exe",
                @"C:\work\shop",
                null,
                CancellationToken.None));
        Assert.Multiple(() =>
        {
            Assert.That(lease.CloseCount, Is.EqualTo(1));
            Assert.That(lease.AcquireCount, Is.EqualTo(1));
        });
    }

    private sealed class RecordingLease : IDesktopWorkspaceLease
    {
        private readonly Queue<bool> _results;

        public RecordingLease(params bool[] results) =>
            _results = new Queue<bool>(results);

        public int CloseCount { get; private set; }
        public int AcquireCount { get; private set; }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            CloseCount++;
            return Task.CompletedTask;
        }

        public Task<bool> TryAcquireAsync(string workspacePath, CancellationToken cancellationToken)
        {
            AcquireCount++;
            return Task.FromResult(_results.Count > 0 && _results.Dequeue());
        }
    }

    private sealed class RecordingLauncher : IDesktopApplicationLauncher
    {
        public Exception? Failure { get; set; }
        public string? WorkspacePath { get; private set; }

        public void Launch(string executablePath, string workspacePath)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            WorkspacePath = workspacePath;
        }
    }
}
