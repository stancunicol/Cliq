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
        Resources[typeof(TextBox)] = InputStyle<TextBox>(inputBackground, foreground, border);
        Resources[typeof(PasswordBox)] = InputStyle<PasswordBox>(inputBackground, foreground, border);
        var comboStyle = InputStyle<ComboBox>(comboBackground, comboForeground, border);
        comboStyle.Setters.Add(new Setter(ComboBox.TemplateProperty, CliqComboBoxTemplate.Create()));
        Resources[typeof(ComboBox)] = comboStyle;
        Resources[typeof(ListBox)] = InputStyle<ListBox>(inputBackground, foreground, border);
        Resources[typeof(Button)] = StyleFor<Button>(
            new Setter(Control.BackgroundProperty, buttonBackground),
            new Setter(Control.ForegroundProperty, foreground),
            new Setter(Control.BorderBrushProperty, border));
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

    private static SolidColorBrush Brush(ThemeResourceKey key)
    {
        var color = VSColorTheme.GetThemedColor(key);
        return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }
}
