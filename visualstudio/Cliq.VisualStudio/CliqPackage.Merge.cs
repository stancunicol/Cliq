using System.Collections.Generic;
using System.Threading.Tasks;
using Cliq.Domain;

namespace Cliq.VisualStudio;

public sealed partial class CliqPackage
{
    internal async Task OpenMergeAsync(IReadOnlyList<PendingCustomMethodMerge>? pendingMerges = null)
    {
        if (Merge is null)
        {
            return;
        }

        if (pendingMerges is not null)
        {
            Merge.LoadPendingMerges(pendingMerges);
        }

        await ShowToolWindowAsync(typeof(CliqMergeToolWindow), 0, create: true, DisposalToken);
    }
}
