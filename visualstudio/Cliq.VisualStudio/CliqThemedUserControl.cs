using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

/// <summary>
/// Base for a Cliq tool-window UserControl that re-themes itself from VS's live color theme and
/// fire-and-forgets WPF event handlers onto the JoinableTaskFactory - the same lifecycle plumbing
/// MergeControl, RunResultsControl, and WorkbenchControl each reimplemented independently. Mirrors
/// <see cref="CliqDialogWindow"/>, which solves the same theming problem for Window-derived dialogs.
/// </summary>
public abstract class CliqThemedUserControl : UserControl
{
    protected CliqThemedUserControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ApplyTheme();
    }

    /// <summary>The FileAndForget activity tag this control's operations are logged under (e.g.
    /// "Cliq/Merge") - kept per-subclass since VS's activity log uses it to distinguish where a
    /// fire-and-forget failure came from.</summary>
    protected abstract string DiagnosticTag { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        OnControlLoaded();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        VSColorTheme.ThemeChanged -= OnThemeChanged;

    private void OnThemeChanged(ThemeChangedEventArgs e) => ApplyTheme();

    /// <summary>Called at the end of every Loaded event, after the theme has been (re)applied - a
    /// subclass that needs to kick off work when it becomes visible (MergeControl's waiting-list
    /// refresh, WorkbenchControl's one-time initial load) overrides this instead of subscribing to
    /// Loaded itself. No-op by default, since not every control needs one (RunResultsControl).</summary>
    protected virtual void OnControlLoaded()
    {
    }

    /// <summary>(re)populates this control's named theme brushes into <see cref="FrameworkElement.Resources"/>
    /// via <see cref="SetBrush"/> - what's actually themed differs per control, so each subclass
    /// supplies its own set.</summary>
    protected abstract void ApplyTheme();

    protected void SetBrush(string key, ThemeResourceKey colorKey, double opacity = 1)
    {
        var color = VSColorTheme.GetThemedColor(colorKey);
        Resources[key] = new SolidColorBrush(
            Color.FromArgb(color.A, color.R, color.G, color.B))
        {
            Opacity = opacity,
        };
    }

    protected void SetBrandBrush(string key, byte red, byte green, byte blue, double opacity = 1)
    {
        Resources[key] = new SolidColorBrush(Color.FromArgb(255, red, green, blue))
        {
            Opacity = opacity,
        };
    }

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "The WPF event intentionally starts an observed joinable task.")]
    protected void RunOperation(Func<Task> operation)
    {
        ThreadHelper.JoinableTaskFactory
            .RunAsync(operation)
            .FileAndForget(DiagnosticTag);
    }
}
