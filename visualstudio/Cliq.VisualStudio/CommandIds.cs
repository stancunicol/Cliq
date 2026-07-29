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
}
