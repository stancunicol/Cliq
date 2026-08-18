using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

internal abstract class CliqDialogWindow : Window
{
    protected CliqDialogWindow()
    {
        Loaded += OnLoaded;
        Closed += OnClosed;
        ApplyTheme();
    }

    /// <summary>Owns the current main window and shows <paramref name="dialog"/> modally -
    /// collapses the "new XyzDialog(...) { Owner = Application.Current?.MainWindow };
    /// dialog.ShowDialog()" shape repeated at every call site that opens one of these dialogs.
    /// Any construction (setting properties, subscribing events) still happens on the caller's
    /// own dialog instance before calling this - only the owning and showing is shared.</summary>
    public static bool? ShowOwned(CliqDialogWindow dialog)
    {
        dialog.Owner = Application.Current?.MainWindow;
        return dialog.ShowDialog();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void OnClosed(object? sender, EventArgs e) =>
        VSColorTheme.ThemeChanged -= OnThemeChanged;

    private void OnThemeChanged(ThemeChangedEventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        var background = Brush(EnvironmentColors.ToolWindowBackgroundColorKey);
        var foreground = Brush(EnvironmentColors.ToolWindowTextColorKey);
        var inputBackground = Brush(EnvironmentColors.ToolWindowBackgroundColorKey);
        var border = Brush(EnvironmentColors.ToolWindowBorderColorKey);
        var buttonBackground = Brush(EnvironmentColors.CommandBarGradientMiddleColorKey);
        var comboBackground = Brush(EnvironmentColors.ComboBoxBackgroundColorKey);
        var comboForeground = Brush(EnvironmentColors.ComboBoxTextColorKey);

        Background = background;
        Foreground = foreground;
        Resources[typeof(TextBlock)] = StyleFor<TextBlock>(
            new Setter(TextBlock.ForegroundProperty, foreground));
        Resources[typeof(Label)] = StyleFor<Label>(
            new Setter(Control.ForegroundProperty, foreground));
        Resources[typeof(CheckBox)] = StyleFor<CheckBox>(
            new Setter(Control.ForegroundProperty, foreground));
        var textBoxStyle = InputStyle<TextBox>(inputBackground, foreground, border);
        textBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, CliqInputTemplate.Create(typeof(TextBox))));
        Resources[typeof(TextBox)] = textBoxStyle;
        var passwordBoxStyle = InputStyle<PasswordBox>(inputBackground, foreground, border);
        passwordBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, CliqInputTemplate.Create(typeof(PasswordBox))));
        Resources[typeof(PasswordBox)] = passwordBoxStyle;
        var comboStyle = InputStyle<ComboBox>(comboBackground, comboForeground, border);
        comboStyle.Setters.Add(new Setter(ComboBox.TemplateProperty, CliqComboBoxTemplate.Create()));
        Resources[typeof(ComboBox)] = comboStyle;
        Resources[typeof(ListBox)] = InputStyle<ListBox>(inputBackground, foreground, border);
        Resources[typeof(Button)] = StyleFor<Button>(
            new Setter(Control.BackgroundProperty, buttonBackground),
            new Setter(Control.ForegroundProperty, foreground),
            new Setter(Control.BorderBrushProperty, border),
            new Setter(Control.TemplateProperty, CliqButtonTemplate.Create()));

        OnThemeApplied();
    }

    /// <summary>
    /// Hook for a subclass with custom, imperatively-built elements (colors this base class's
    /// implicit type-based styles above don't reach) to re-theme itself alongside the rest of the
    /// window, instead of hardcoding literal colors that would stay wrong across VS's light/dark/
    /// high-contrast themes. Runs on every <see cref="ApplyTheme"/> call, including the one from
    /// this class's own constructor - an override must only populate named <see cref="Resources"/>
    /// entries here (picked up later via <see cref="FrameworkElement.SetResourceReference"/> on
    /// whatever elements the subclass creates), not touch subclass instance fields, since those
    /// aren't initialized yet the first time this runs.
    /// </summary>
    protected virtual void OnThemeApplied()
    {
    }

    private static Style InputStyle<T>(Brush background, Brush foreground, Brush border)
        where T : Control =>
        StyleFor<T>(
            new Setter(Control.BackgroundProperty, background),
            new Setter(Control.ForegroundProperty, foreground),
            new Setter(Control.BorderBrushProperty, border));

    private static Style StyleFor<T>(params Setter[] setters) where T : FrameworkElement
    {
        var style = new Style(typeof(T));
        foreach (var setter in setters)
        {
            style.Setters.Add(setter);
        }

        return style;
    }

    protected static SolidColorBrush Brush(ThemeResourceKey key)
    {
        var color = VSColorTheme.GetThemedColor(key);
        return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }
}
