using System;
using System.IO;

namespace Cliq.VisualStudio.Core.Shell;

public enum CliqShellCommand
{
    OpenWorkbench,
    ScanPage,
    RunSelectedTest,
    GenerateSelected,
    GenerateAll,
    RecordInDesktop,
    Refresh,
    Settings,
    BindWorkspace,
    OpenScopedWorkbench,
    GenerateScopedAll,
    BindProjectAndOpen,
    OpenCliqDocument,
    RunCliqTest,
    GenerateCliqDocument,
    RescanCliqPage,
    ScanSelectedUrl,
    OpenMerge,
    RegenerateFile,
    LocateElementFromSource
}

public readonly struct CliqCommandState
{
    public CliqCommandState(bool visible, bool enabled, string? text = null)
    {
        Visible = visible;
        Enabled = enabled;
        Text = text;
    }

    public bool Visible { get; }
    public bool Enabled { get; }
    public string? Text { get; }

    public static CliqCommandState Hidden => new(false, false);
    public static CliqCommandState Available(string? text = null) => new(true, true, text);
    public static CliqCommandState Disabled(string? text = null) => new(true, false, text);
}

public static class CliqShellContext
{
    public static CliqCommandState Evaluate(CliqShellCommand command, IdeContextSnapshot context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var hasWorkspaceScope = WorkspacePathResolver.ResolveForSelectedScope(context) is not null;
        var documentKind = context.SelectedDocumentKind;
        var isTest = documentKind == CliqDocumentKind.Test;
        var isPage = documentKind == CliqDocumentKind.Page;
        var isCliqDocument = isTest || isPage;
        var isSolutionOrProject =
            context.SelectionKind == IdeSelectionKind.Solution ||
            context.SelectionKind == IdeSelectionKind.Project;
        var isNormalFile =
            context.SelectionKind == IdeSelectionKind.File &&
            !isCliqDocument &&
            context.SelectedItemPath is not null;
        var hasUrl = CliqDocumentClassifier.TryParseHttpSelection(context.SelectedText, out _);
        var isCSharpFile = context.SelectedItemPath is not null &&
            string.Equals(Path.GetExtension(context.SelectedItemPath), ".cs", StringComparison.OrdinalIgnoreCase);

        switch (command)
        {
            case CliqShellCommand.OpenWorkbench:
                return CliqCommandState.Available();
            case CliqShellCommand.ScanPage:
                return hasWorkspaceScope
                    ? CliqCommandState.Available()
                    : CliqCommandState.Disabled();
            case CliqShellCommand.RunSelectedTest:
                return hasWorkspaceScope
                    ? CliqCommandState.Available()
                    : CliqCommandState.Disabled();
            case CliqShellCommand.GenerateSelected:
                return hasWorkspaceScope
                    ? CliqCommandState.Available()
                    : CliqCommandState.Disabled();
            case CliqShellCommand.GenerateAll:
            case CliqShellCommand.RecordInDesktop:
            case CliqShellCommand.Refresh:
                return hasWorkspaceScope ? CliqCommandState.Available() : CliqCommandState.Disabled();
            case CliqShellCommand.Settings:
            case CliqShellCommand.OpenMerge:
                return CliqCommandState.Available();
            case CliqShellCommand.BindWorkspace:
            case CliqShellCommand.OpenScopedWorkbench:
            case CliqShellCommand.GenerateScopedAll:
                return isSolutionOrProject && hasWorkspaceScope
                    ? CliqCommandState.Available()
                    : CliqCommandState.Hidden;
            case CliqShellCommand.BindProjectAndOpen:
                return isNormalFile && context.SelectedProjectDirectory is not null
                    ? CliqCommandState.Available()
                    : CliqCommandState.Hidden;
            case CliqShellCommand.RegenerateFile:
                // Scoped to .cs files rather than every normal file (unlike BindProjectAndOpen
                // above) - a right-click "Regenerate" only makes sense on the generated Page
                // Object/test files themselves, not an arbitrary project file.
                return isNormalFile && isCSharpFile && context.SelectedProjectDirectory is not null
                    ? CliqCommandState.Available()
                    : CliqCommandState.Hidden;
            case CliqShellCommand.OpenCliqDocument:
            case CliqShellCommand.GenerateCliqDocument:
                return isCliqDocument ? CliqCommandState.Available() : CliqCommandState.Hidden;
            case CliqShellCommand.RunCliqTest:
                return isTest ? CliqCommandState.Available() : CliqCommandState.Hidden;
            case CliqShellCommand.RescanCliqPage:
                return isPage ? CliqCommandState.Available() : CliqCommandState.Hidden;
            case CliqShellCommand.ScanSelectedUrl:
                return hasUrl
                    ? CliqCommandState.Available()
                    : CliqCommandState.Hidden;
            case CliqShellCommand.LocateElementFromSource:
                return CliqDocumentClassifier.ResolveLocatorFieldComment(context.SelectedText, context.LineAboveSelection) is not null
                    ? CliqCommandState.Available()
                    : CliqCommandState.Hidden;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }
}
