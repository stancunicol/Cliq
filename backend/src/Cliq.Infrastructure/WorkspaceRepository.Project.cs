using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository
{
    public async Task<ProjectManifest> SaveProjectAsync(ProjectManifest project, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(project.SchemaVersion, ProjectManifest.CurrentSchemaVersion, "project");
        ValidateRelativeDirectory(project.Output.PageObjectsDirectory, nameof(project.Output.PageObjectsDirectory));
        ValidateRelativeDirectory(project.Output.TestsDirectory, nameof(project.Output.TestsDirectory));
        var updated = project with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(ProjectPath, updated, cancellationToken);
        return updated;
    }
}
