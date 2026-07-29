using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cliq.VisualStudio.Core;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

[Guid("3B601536-F528-412B-BD35-A44059331B25")]
public sealed class CliqWorkbenchToolWindow : ToolWindowPane
{
    public CliqWorkbenchToolWindow()
        : base(null)
    {
        Caption = CliqExtensionConstants.WorkbenchTitle;
        Content = new CliqPlaceholderControl(
            CliqExtensionConstants.WorkbenchTitle,
            "Test and page authoring will appear here.");
    }
}

[Guid("4F54B430-4DBF-4DBA-B56C-F922B5094B48")]
public sealed class CliqRunResultsToolWindow : ToolWindowPane
{
    public CliqRunResultsToolWindow()
        : base(null)
    {
        Caption = CliqExtensionConstants.RunResultsTitle;
        Content = new CliqPlaceholderControl(
            CliqExtensionConstants.RunResultsTitle,
            "Live run progress and results will appear here.");
    }
}

internal sealed class CliqPlaceholderControl : Grid
{
    private readonly TextBlock _title;
    private readonly TextBlock _description;

    public CliqPlaceholderControl(string title, string description)
    {
        Margin = new Thickness(16);
        System.Windows.Automation.AutomationProperties.SetName(this, title);
        System.Windows.Automation.AutomationProperties.SetHelpText(this, description);

        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition());

        _title = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
        System.Windows.Automation.AutomationProperties.SetName(_title, title);

        _description = new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(_description, 1);

        Children.Add(_title);
        Children.Add(_description);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ApplyTheme();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
        var foreground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
        Background = new SolidColorBrush(ToMediaColor(background));
        _title.Foreground = new SolidColorBrush(ToMediaColor(foreground));
        _description.Foreground = new SolidColorBrush(ToMediaColor(foreground));
    }

    private static Color ToMediaColor(System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
