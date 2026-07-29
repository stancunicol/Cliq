using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cliq.VisualStudio;

internal sealed class SecretValueDialog : Window
{
    private readonly TextBox _key;
    private readonly PasswordBox _value;

    public SecretValueDialog(string? existingKey)
    {
        Title = existingKey == null ? "Add Cliq secret" : "Update Cliq secret";
        Width = 440;
        Height = 235;
        MinWidth = 360;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = SystemColors.WindowBrush;
        Foreground = SystemColors.WindowTextBrush;

        _key = new TextBox
        {
            Text = existingKey ?? string.Empty,
            IsReadOnly = existingKey != null,
            Margin = new Thickness(0, 4, 0, 12),
            Padding = new Thickness(6, 4, 6, 4),
        };
        AutomationProperties.SetName(_key, "Secret key");

        _value = new PasswordBox
        {
            Margin = new Thickness(0, 4, 0, 16),
            Padding = new Thickness(6, 4, 6, 4),
        };
        AutomationProperties.SetName(_value, "Secret value");
        AutomationProperties.SetHelpText(
            _value,
            "The value is masked and is not retained by the editor.");

        var save = new Button
        {
            Content = existingKey == null ? "Add secret" : "Update value",
            IsDefault = true,
            MinWidth = 90,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(6, 0, 0, 0),
        };
        save.Click += OnSave;
        AutomationProperties.SetName(save, "Save secret value");

        var cancel = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 75,
            Padding = new Thickness(10, 5, 10, 5),
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);

        var content = new StackPanel
        {
            Margin = new Thickness(18),
        };
        content.Children.Add(new TextBlock
        {
            Text = "Secret key",
            FontWeight = FontWeights.SemiBold,
        });
        content.Children.Add(_key);
        content.Children.Add(new TextBlock
        {
            Text = "Secret value",
            FontWeight = FontWeights.SemiBold,
        });
        content.Children.Add(_value);
        content.Children.Add(buttons);
        Content = content;

        Loaded += (_, _) =>
        {
            if (existingKey == null)
            {
                _key.Focus();
            }
            else
            {
                _value.Focus();
            }
        };
    }

    public string SecretKey => _key.Text.Trim();

    public string TakeSecret()
    {
        var result = _value.Password;
        _value.Clear();
        return result;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_key.Text))
        {
            MessageBox.Show(
                this,
                "Enter a secret key.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _key.Focus();
            return;
        }

        if (_value.SecurePassword.Length == 0)
        {
            MessageBox.Show(
                this,
                "Enter a secret value.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _value.Focus();
            return;
        }

        DialogResult = true;
    }
}
