using System;

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
    /// <summary>
    /// <paramref name="hasSelectedTest"/>/<paramref name="hasSelectedPage"/> report whether the
    /// Workbench (not the IDE document/selection <paramref name="context"/> otherwise carries)
    /// currently has an explicit test/page selection - the same signal
    /// CliqPackage.RunSelectedTestAsync/GenerateSelectedAsync fall back to at execute time when
    /// the active document isn't a Cliq test/page itself. Both default to false so every other
    /// command's evaluation (and every existing caller) is unaffected; only RunSelectedTest/
    /// GenerateSelected read them, so their toolbar/menu entries are enabled exactly when there is
    /// something real to act on - not merely because a workspace happens to be bound - matching
    /// execute-time behavior instead of only failing with a message after the click.
    /// </summary>
    public static CliqCommandState Evaluate(
        CliqShellCommand command,
        IdeContextSnapshot context,
        bool hasSelectedTest = false,
        bool hasSelectedPage = false)
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

        switch (command)
        {
            case CliqShellCommand.OpenWorkbench:
                return CliqCommandState.Available();
            case CliqShellCommand.ScanPage:
                return hasWorkspaceScope
                    ? CliqCommandState.Available()
                    : CliqCommandState.Disabled();
            case CliqShellCommand.RunSelectedTest:
                // Mirrors RunSelectedTestAsync's own resolution exactly: the active document must
                // be a Cliq test, or the Workbench needs an explicit test selection - nothing else
                // gives it a test to run.
                return hasWorkspaceScope && (isTest || hasSelectedTest)
                    ? CliqCommandState.Available()
                    : CliqCommandState.Disabled();
            case CliqShellCommand.GenerateSelected:
                // Mirrors GenerateSelectedAsync's own resolution: the active document is a Cliq
                // test/page, or the Workbench has an explicit test or page selection.
                return hasWorkspaceScope && (isCliqDocument || hasSelectedTest || hasSelectedPage)
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
