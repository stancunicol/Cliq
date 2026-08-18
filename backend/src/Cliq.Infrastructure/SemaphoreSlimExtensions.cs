namespace Cliq.Infrastructure;

/// <summary>
/// Collapses the repeated "WaitAsync, try, Release in finally" shape into a single call so a
/// future method can't forget the <c>finally</c> and leak the gate.
/// </summary>
internal static class SemaphoreSlimExtensions
{
    public static async Task<T> WithLockAsync<T>(
        this SemaphoreSlim gate,
        Func<Task<T>> body,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await body();
        }
        finally
        {
            gate.Release();
        }
    }

    public static async Task WithLockAsync(
        this SemaphoreSlim gate,
        Func<Task> body,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await body();
        }
        finally
        {
            gate.Release();
        }
    }
}
