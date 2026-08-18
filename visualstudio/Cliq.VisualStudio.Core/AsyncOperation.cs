using System;
using System.Threading.Tasks;

namespace Cliq.VisualStudio.Core;

/// <summary>
/// The busy/status/error/cancellation plumbing every "run one async action, report status or
/// error, clear busy when done" wrapper in this project needs - factored out so each view-model's
/// own RunAsync-style method only supplies what's genuinely different about it: how it decides an
/// operation is already in progress (throw, queue, or reject), and any operation-specific exception
/// handling it layers on top (composed into the <c>operation</c> delegate itself, so it runs before
/// this class's own generic catch ever sees it).
///
/// Deliberately not a base class or an object owning the busy/status/error state itself - each
/// view-model keeps its own public IsBusy/StatusMessage/ErrorMessage properties (so XAML bindings
/// like <c>{Binding StatusMessage}</c> keep pointing at the view-model itself, not a nested object),
/// and passes them in as setters.
///
/// Two shapes per return type: one takes an upfront <c>status</c> message to show before the
/// operation starts (what most callers want), the other omits it - <c>WorkbenchViewModel</c>'s
/// wrapper never touches StatusMessage until the action itself sets one, and forcing an upfront
/// message (even null) onto it would blank out whatever the previous operation left behind.
/// </summary>
internal static class AsyncOperation
{
    public static Task<T?> RunAsync<T>(
        Action<bool> setBusy,
        Action<string?> setStatus,
        Action<string?> setError,
        string status,
        Func<Task<T>> operation)
    {
        setStatus(status);
        return RunAsync(setBusy, setStatus, setError, operation);
    }

    public static async Task<T?> RunAsync<T>(
        Action<bool> setBusy,
        Action<string?> setStatus,
        Action<string?> setError,
        Func<Task<T>> operation)
    {
        setBusy(true);
        setError(null);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            setStatus("Operation cancelled.");
            return default;
        }
        catch (Exception exception)
        {
            setError(exception.Message);
            return default;
        }
        finally
        {
            setBusy(false);
        }
    }

    public static Task<bool> RunAsync(
        Action<bool> setBusy,
        Action<string?> setStatus,
        Action<string?> setError,
        string status,
        Func<Task> operation)
    {
        setStatus(status);
        return RunAsync(setBusy, setStatus, setError, operation);
    }

    /// <summary>
    /// The "reject if a second call comes in while one is already running" guard that every
    /// tool-window view-model's own RunAsync wrapper (MergeViewModel, ScanReviewViewModel) used to
    /// duplicate by hand before delegating to <see cref="RunAsync{T}(Action{bool}, Action{string}, Action{string}, string, Func{Task{T}})"/> -
    /// factored out here so a caller only supplies how busy-ness is read, not the check itself.
    /// </summary>
    public static Task<T?> RunGuardedAsync<T>(
        Func<bool> isBusy,
        Action<bool> setBusy,
        Action<string?> setStatus,
        Action<string?> setError,
        string status,
        Func<Task<T>> operation)
    {
        if (isBusy())
        {
            setError("Another Cliq action is already running.");
            return Task.FromResult<T?>(default);
        }

        return RunAsync(setBusy, setStatus, setError, status, operation);
    }

    public static async Task<bool> RunAsync(
        Action<bool> setBusy,
        Action<string?> setStatus,
        Action<string?> setError,
        Func<Task> operation)
    {
        setBusy(true);
        setError(null);
        try
        {
            await operation().ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            setStatus("Operation cancelled.");
            return false;
        }
        catch (Exception exception)
        {
            setError(exception.Message);
            return false;
        }
        finally
        {
            setBusy(false);
        }
    }
}
