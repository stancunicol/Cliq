using Cliq.VisualStudio.Core;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class AsyncOperationTests
{
    [Test]
    public async Task RunAsync_SetsBusyErrorNullThenRunsAndClearsBusy()
    {
        var busyStates = new List<bool>();
        var statuses = new List<string?>();
        var errors = new List<string?>();

        var result = await AsyncOperation.RunAsync(
            value => busyStates.Add(value),
            value => statuses.Add(value),
            value => errors.Add(value),
            "Working...",
            async () =>
            {
                await Task.Yield();
                return 42;
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(42));
            Assert.That(busyStates, Is.EqualTo(new[] { true, false }));
            Assert.That(statuses, Is.EqualTo(new[] { "Working..." }));
            Assert.That(errors, Is.EqualTo(new[] { (string?)null }));
        });
    }

    [Test]
    public async Task RunAsync_NoUpfrontStatus_LeavesStatusUntouched()
    {
        var statuses = new List<string?> { "previous status" };
        var errors = new List<string?>();

        await AsyncOperation.RunAsync(
            _ => { },
            value => statuses.Add(value),
            value => errors.Add(value),
            async () => { await Task.Yield(); });

        Assert.Multiple(() =>
        {
            Assert.That(statuses, Is.EqualTo(new[] { "previous status" }));
            Assert.That(errors, Is.EqualTo(new[] { (string?)null }));
        });
    }

    [Test]
    public async Task RunAsync_Generic_OnException_ReturnsDefaultAndReportsError()
    {
        var errors = new List<string?>();
        var busyStates = new List<bool>();

        var result = await AsyncOperation.RunAsync(
            value => busyStates.Add(value),
            _ => { },
            value => errors.Add(value),
            "Working...",
            () => Task.FromException<int>(new InvalidOperationException("boom")));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(errors, Is.EqualTo(new[] { (string?)null, "boom" }));
            Assert.That(busyStates, Is.EqualTo(new[] { true, false }), "busy must be cleared even on failure");
        });
    }

    [Test]
    public async Task RunAsync_OnCancellation_ReportsCancelledStatusAndReturnsDefault()
    {
        var statuses = new List<string?>();

        var result = await AsyncOperation.RunAsync(
            _ => { },
            value => statuses.Add(value),
            _ => { },
            "Working...",
            () => Task.FromCanceled<int>(new CancellationToken(true)));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(statuses, Is.EqualTo(new[] { "Working...", "Operation cancelled." }));
        });
    }

    [Test]
    public async Task RunAsync_Bool_ReturnsTrueOnSuccessAndFalseOnException()
    {
        var successful = await AsyncOperation.RunAsync(
            _ => { }, _ => { }, _ => { },
            async () => { await Task.Yield(); });

        var failed = await AsyncOperation.RunAsync(
            _ => { }, _ => { }, _ => { },
            () => Task.FromException(new InvalidOperationException("boom")));

        Assert.Multiple(() =>
        {
            Assert.That(successful, Is.True);
            Assert.That(failed, Is.False);
        });
    }

    [Test]
    public async Task RunGuardedAsync_WhenAlreadyBusy_RejectsWithoutRunning()
    {
        var errors = new List<string?>();
        var ran = false;

        var result = await AsyncOperation.RunGuardedAsync(
            () => true,
            _ => { },
            _ => { },
            value => errors.Add(value),
            "Working...",
            async () =>
            {
                ran = true;
                await Task.Yield();
                return 7;
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(ran, Is.False);
            Assert.That(errors, Is.EqualTo(new[] { "Another Cliq action is already running." }));
        });
    }

    [Test]
    public async Task RunGuardedAsync_WhenNotBusy_DelegatesToRunAsync()
    {
        var statuses = new List<string?>();
        var result = await AsyncOperation.RunGuardedAsync(
            () => false,
            _ => { },
            value => statuses.Add(value),
            _ => { },
            "Working...",
            async () =>
            {
                await Task.Yield();
                return 9;
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(9));
            Assert.That(statuses, Is.EqualTo(new[] { "Working..." }));
        });
    }
}
