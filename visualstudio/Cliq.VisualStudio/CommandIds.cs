using System;

namespace Cliq.VisualStudio;

internal static class CommandIds
{
    public static readonly Guid CommandSet = new("D758461A-EBBA-4509-BC70-FA99B43E3942");

    public const int OpenWorkbench = 0x0100;
    public const int ScanPage = 0x0101;
    public const int RunSelectedTest = 0x0102;
    public const int GenerateSelected = 0x0103;
    public const int GenerateAll = 0x0104;
    public const int RecordInDesktop = 0x0105;
    public const int Refresh = 0x0106;
    public const int Settings = 0x0107;
    public const int BindWorkspace = 0x0200;
    public const int OpenScopedWorkbench = 0x0201;
    public const int GenerateScopedAll = 0x0202;
    public const int BindProjectAndOpen = 0x0203;
    public const int OpenCliqDocument = 0x0204;
    public const int RunCliqTest = 0x0205;
    public const int GenerateCliqDocument = 0x0206;
    public const int RescanCliqPage = 0x0207;
    public const int ScanSelectedUrl = 0x0208;
}
