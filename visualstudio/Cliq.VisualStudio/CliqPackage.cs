using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(
    "Cliq for Visual Studio",
    "Integrates Cliq UI test automation into Visual Studio.",
    "0.1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(CliqWorkbenchToolWindow))]
[ProvideToolWindow(typeof(CliqRunResultsToolWindow))]
[Guid(PackageGuidString)]
public sealed class CliqPackage : AsyncPackage
{
    public const string PackageGuidString = "2C0AF2DA-640F-41B8-B02D-E847AF06C29E";

    protected override Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        return Task.CompletedTask;
    }
}
