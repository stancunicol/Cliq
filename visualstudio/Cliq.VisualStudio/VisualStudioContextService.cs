using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace Cliq.VisualStudio;

internal sealed class VisualStudioContextService : IVsSolutionEvents, IDisposable
{
    private const string OverridesCollection = "Cliq\\WorkspaceOverrides";
    private readonly AsyncPackage _package;
    private IVsSolution? _solution;
    private IVsMonitorSelection? _selection;
    private DTE2? _dte;
    private uint _solutionEventsCookie;

    public VisualStudioContextService(AsyncPackage package)
    {
        _package = package;
    }

    public event EventHandler? ContextChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _solution = await _package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        _selection = await _package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
        _dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (_solution is not null)
        {
            ErrorHandler.ThrowOnFailure(_solution.AdviseSolutionEvents(this, out _solutionEventsCookie));
        }
    }

    public IdeContextSnapshot Capture()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var solutionDirectory = GetSolutionDirectory();
        var selectedProjectDirectory = default(string);
        var selectedItemPath = default(string);
        var selectionKind = IdeSelectionKind.Unknown;

        if (_selection is not null &&
            ErrorHandler.Succeeded(_selection.GetCurrentSelection(
                out var hierarchyPointer,
                out var itemId,
                out _,
                out var selectionContainerPointer)))
        {
            try
            {
                if (hierarchyPointer != IntPtr.Zero)
                {
                    var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy;
                    selectedItemPath = GetCanonicalName(hierarchy, itemId);
                    selectedProjectDirectory = GetProjectDirectory(hierarchy);
                    selectionKind = ClassifySelection(itemId, selectedItemPath, selectedProjectDirectory);
                }
            }
            finally
            {
                if (hierarchyPointer != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPointer);
                }

                if (selectionContainerPointer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPointer);
                }
            }
        }

        var activeDocumentPath = GetActiveDocumentPath();
        var selectedText = GetSelectedText();
        var lineAboveSelection = GetLineAboveSelection();
        if (selectionKind == IdeSelectionKind.Unknown && activeDocumentPath is not null)
        {
            selectionKind = IdeSelectionKind.Editor;
        }
        else if (selectionKind == IdeSelectionKind.Unknown && solutionDirectory is not null)
        {
            selectionKind = IdeSelectionKind.Solution;
        }

        return new IdeContextSnapshot(
            solutionDirectory,
            selectedProjectDirectory,
            selectedItemPath,
            activeDocumentPath,
            selectedText,
            selectionKind,
            lineAboveSelection);
    }

    public string? GetOverride(IdeContextSnapshot context)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var identity = context.SolutionDirectory;
        if (identity is null)
        {
            return null;
        }

        var store = new ShellSettingsManager(_package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
        var property = WorkspacePathResolver.OverrideKey(identity);
        return store.CollectionExists(OverridesCollection) && store.PropertyExists(OverridesCollection, property)
            ? store.GetString(OverridesCollection, property)
            : null;
    }

    public void SetOverride(IdeContextSnapshot context, string? workspacePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var identity = context.SolutionDirectory
            ?? throw new InvalidOperationException("A solution or open folder is required to persist a workspace override.");
        var store = new ShellSettingsManager(_package).GetWritableSettingsStore(SettingsScope.UserSettings);
        if (!store.CollectionExists(OverridesCollection))
        {
            store.CreateCollection(OverridesCollection);
        }

        var property = WorkspacePathResolver.OverrideKey(identity);
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            if (store.PropertyExists(OverridesCollection, property))
            {
                store.DeleteProperty(OverridesCollection, property);
            }

            return;
        }

        store.SetString(OverridesCollection, property, Path.GetFullPath(workspacePath));
    }

    public void Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_solution is not null && _solutionEventsCookie != 0)
        {
            _solution.UnadviseSolutionEvents(_solutionEventsCookie);
            _solutionEventsCookie = 0;
        }
    }

    private string? GetSolutionDirectory()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_solution is null ||
            ErrorHandler.Failed(_solution.GetSolutionInfo(out var directory, out var solutionFile, out _)))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            return directory;
        }

        return string.IsNullOrWhiteSpace(solutionFile) ? null : Path.GetDirectoryName(solutionFile);
    }

    private static string? GetCanonicalName(IVsHierarchy? hierarchy, uint itemId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return hierarchy is not null &&
               itemId != VSConstants.VSITEMID_NIL &&
               ErrorHandler.Succeeded(hierarchy.GetCanonicalName(itemId, out var canonicalName))
            ? canonicalName
            : null;
    }

    private static string? GetProjectDirectory(IVsHierarchy? hierarchy)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (hierarchy is null ||
            ErrorHandler.Failed(hierarchy.GetProperty(
                VSConstants.VSITEMID_ROOT,
                (int)__VSHPROPID.VSHPROPID_ProjectDir,
                out var value)))
        {
            return null;
        }

        return value as string;
    }

    private static IdeSelectionKind ClassifySelection(
        uint itemId,
        string? selectedItemPath,
        string? selectedProjectDirectory)
    {
        if (itemId == VSConstants.VSITEMID_ROOT)
        {
            return string.Equals(Path.GetExtension(selectedItemPath), ".sln", StringComparison.OrdinalIgnoreCase) ||
                   selectedProjectDirectory is null
                ? IdeSelectionKind.Solution
                : IdeSelectionKind.Project;
        }

        return selectedItemPath is null
            ? IdeSelectionKind.Unknown
            : IdeSelectionKind.File;
    }

    private string? GetActiveDocumentPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            return _dte?.ActiveDocument?.FullName;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private string? GetSelectedText()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            return (_dte?.ActiveDocument?.Selection as TextSelection)?.Text;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private string? GetLineAboveSelection()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            var activePoint = (_dte?.ActiveDocument?.Selection as TextSelection)?.ActivePoint;
            if (activePoint is null || activePoint.Line <= 1)
            {
                return null;
            }

            var editPoint = activePoint.CreateEditPoint();
            return editPoint.GetLines(activePoint.Line - 1, activePoint.Line);
        }
        catch (COMException)
        {
            return null;
        }
    }

    public int OnAfterOpenProject(IVsHierarchy hierarchy, int added) => Notify();
    public int OnQueryCloseProject(IVsHierarchy hierarchy, int removing, ref int cancel) => VSConstants.S_OK;
    public int OnBeforeCloseProject(IVsHierarchy hierarchy, int removed) => VSConstants.S_OK;
    public int OnAfterLoadProject(IVsHierarchy stubHierarchy, IVsHierarchy realHierarchy) => Notify();
    public int OnQueryUnloadProject(IVsHierarchy realHierarchy, ref int cancel) => VSConstants.S_OK;
    public int OnBeforeUnloadProject(IVsHierarchy realHierarchy, IVsHierarchy stubHierarchy) => VSConstants.S_OK;
    public int OnAfterOpenSolution(object reserved, int newSolution) => Notify();
    public int OnQueryCloseSolution(object reserved, ref int cancel) => VSConstants.S_OK;
    public int OnBeforeCloseSolution(object reserved) => VSConstants.S_OK;
    public int OnAfterCloseSolution(object reserved) => Notify();

    private int Notify()
    {
        ContextChanged?.Invoke(this, EventArgs.Empty);
        return VSConstants.S_OK;
    }
}
