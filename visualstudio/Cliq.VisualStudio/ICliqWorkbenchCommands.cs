using System.Threading.Tasks;
using Cliq.VisualStudio.Core.Actions;

namespace Cliq.VisualStudio;

/// <summary>
/// The subset of <see cref="CliqPackage"/> that <see cref="WorkbenchControl"/> needs to trigger
/// package-level workflows from its click handlers - injected instead of reached for via the
/// <see cref="CliqPackage.Current"/> static singleton, so the control depends on an abstraction
/// small enough to fake in a unit test rather than the whole package.
/// </summary>
internal interface ICliqWorkbenchCommands
{
    Task GenerateSelectedAsync();

    Task ScanPageAsync();

    Task OpenGeneratedPageObjectAsync(string pageId);

    /// <summary>Whether <paramref name="pageId"/> already has a generated Page Object file on
    /// disk - drives the Page Inspector's "Generate"/"Regenerate" button label (see
    /// WorkbenchControl.xaml.cs).</summary>
    Task<bool> HasGeneratedPageObjectAsync(string pageId);

    Task RunSelectedTestAsync();

    WorkspaceActionsViewModel? Actions { get; }
}
