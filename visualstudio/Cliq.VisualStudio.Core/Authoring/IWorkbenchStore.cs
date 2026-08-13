using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public interface IWorkbenchStore
{
    Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken);

    Task DeleteTestAsync(string testId, CancellationToken cancellationToken);

    Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken);
}

public interface IWorkspaceConfigurationStore
{
    Task<CliqSettings> LoadSettingsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SecretDescriptor>> LoadSecretsAsync(CancellationToken cancellationToken);

    Task<ProjectManifest> SaveProjectAsync(
        ProjectManifest project,
        CancellationToken cancellationToken);

    Task<CliqSettings> SaveSettingsAsync(
        CliqSettings settings,
        CancellationToken cancellationToken);

    Task SaveSecretAsync(
        string key,
        string plaintextValue,
        CancellationToken cancellationToken);

    Task DeleteSecretAsync(string key, CancellationToken cancellationToken);
}

public sealed class SecretDescriptor
{
    public SecretDescriptor(string key, bool hasValue)
    {
        Key = key;
        HasValue = hasValue;
    }

    public string Key { get; }

    public bool HasValue { get; }
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
