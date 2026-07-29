using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public interface IWorkbenchStore
{
    Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken);

    Task DeleteTestAsync(string testId, CancellationToken cancellationToken);
}

public interface IWorkbenchPrompt
{
    Task<bool> ConfirmDiscardChangesAsync(string operation, CancellationToken cancellationToken);

    Task<bool> ConfirmDeleteTestAsync(string testName, CancellationToken cancellationToken);
}

public sealed class PermissiveWorkbenchPrompt : IWorkbenchPrompt
{
    public Task<bool> ConfirmDiscardChangesAsync(string operation, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ConfirmDeleteTestAsync(string testName, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
