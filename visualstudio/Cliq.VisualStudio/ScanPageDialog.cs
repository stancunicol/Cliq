using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Cliq.Domain;

namespace Cliq.VisualStudio;

internal sealed class ScanPageDialog : CliqDialogWindow
{
    private readonly TextBox _url;
    private readonly TextBox _name;
    private readonly ComboBox _browser;
    private readonly CheckBox _headless;

    public ScanPageDialog(
        string? initialUrl,
        string? initialName,
        BrowserKind defaultBrowser)
    {
        Title = "Scan Page in Cliq";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid { Margin = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var index = 0; index < 5; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        _url = AddField(grid, 0, "_URL:", initialUrl ?? string.Empty);
        _name = AddField(grid, 1, "_Page name:", initialName ?? string.Empty);
        _browser = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(BrowserKind)),
            SelectedItem = defaultBrowser,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 180,
        };
        AutomationProperties.SetName(_browser, "Browser");
        Grid.SetRow(_browser, 2);
        Grid.SetColumn(_browser, 1);
        grid.Children.Add(_browser);
        AddLabel(grid, 2, "_Browser:", _browser);

        _headless = new CheckBox
        {
            Content = "Run scan _headlessly",
            IsChecked = true,
            Margin = new Thickness(8, 8, 0, 8),
        };
        Grid.SetRow(_headless, 3);
        Grid.SetColumn(_headless, 1);
        grid.Children.Add(_headless);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var scan = new Button
        {
            Content = "_Scan",
            IsDefault = true,
            MinWidth = 82,
            Margin = new Thickness(0, 0, 8, 0),
        };
        scan.Click += (_, _) => Accept();
        var cancel = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 82,
        };
        buttons.Children.Add(scan);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 4);
        Grid.SetColumnSpan(buttons, 2);
        grid.Children.Add(buttons);
        Content = grid;
    }

    public string PageUrl => _url.Text.Trim();
    public string PageName => _name.Text.Trim();
    public BrowserKind Browser => (BrowserKind)(_browser.SelectedItem ?? BrowserKind.Chrome);
    public bool Headless => _headless.IsChecked == true;

    private static TextBox AddField(Grid grid, int row, string label, string value)
    {
        var field = new TextBox
        {
            Text = value,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 350,
        };
        AutomationProperties.SetName(
            field,
            label.Replace("_", string.Empty).TrimEnd(':'));
        Grid.SetRow(field, row);
        Grid.SetColumn(field, 1);
        grid.Children.Add(field);
        AddLabel(grid, row, label, field);
        return field;
    }

    private static void AddLabel(Grid grid, int row, string text, UIElement target)
    {
        var label = new Label
        {
            Content = text,
            VerticalAlignment = VerticalAlignment.Center,
            Target = target,
        };
        Grid.SetRow(label, row);
        grid.Children.Add(label);
    }

    private void Accept()
    {
        if (!Uri.TryCreate(PageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            CliqMessageBox.ShowWarning(this, "Enter an absolute HTTP or HTTPS URL.");
            _url.Focus();
            return;
        }

        if (PageName.Length == 0)
        {
            CliqMessageBox.ShowWarning(this, "Enter a page name.");
            _name.Focus();
            return;
        }

        DialogResult = true;
    }
}
