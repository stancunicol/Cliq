using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Authoring;

namespace Cliq.VisualStudio.Core.Api
{
    public interface ICliqApiSession
    {
        Task<CliqApiClient> GetClientAsync(CancellationToken cancellationToken);
    }

    public sealed class ApiWorkbenchStore : IWorkbenchStore, IWorkspaceConfigurationStore
    {
        private readonly ICliqApiSession _session;

        public ApiWorkbenchStore(ICliqApiSession session) =>
            _session = session ?? throw new ArgumentNullException(nameof(session));

        public async Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .GetWorkspaceAsync(cancellationToken).ConfigureAwait(false);

        public async Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .SaveTestAsync(test, cancellationToken).ConfigureAwait(false);

        public async Task DeleteTestAsync(string testId, CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .DeleteTestAsync(testId, cancellationToken).ConfigureAwait(false);

        public async Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .SavePageAsync(page, cancellationToken).ConfigureAwait(false);

        public async Task<CliqSettings> LoadSettingsAsync(CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .GetSettingsAsync(cancellationToken).ConfigureAwait(false);

        public async Task<IReadOnlyList<SecretDescriptor>> LoadSecretsAsync(
            CancellationToken cancellationToken)
        {
            var values = await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .GetSecretsAsync(cancellationToken).ConfigureAwait(false);
            return values.Keys
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .Select(key => new SecretDescriptor(key, true))
                .ToArray();
        }

        public async Task<ProjectManifest> SaveProjectAsync(
            ProjectManifest project,
            CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .SaveProjectAsync(project, cancellationToken).ConfigureAwait(false);

        public async Task<CliqSettings> SaveSettingsAsync(
            CliqSettings settings,
            CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(false);

        public async Task SaveSecretAsync(
            string key,
            string plaintextValue,
            CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [key] = plaintextValue,
            };
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .SaveSecretsAsync(values, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken) =>
            await (await ClientAsync(cancellationToken).ConfigureAwait(false))
                .DeleteSecretAsync(key, cancellationToken).ConfigureAwait(false);

        private Task<CliqApiClient> ClientAsync(CancellationToken cancellationToken) =>
            _session.GetClientAsync(cancellationToken);
    }

}
