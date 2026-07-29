using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cliq.VisualStudio.Core.Runs;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public partial class RunResultsControl : UserControl
{
    private readonly RunResultsViewModel _viewModel;

    public RunResultsControl(RunResultsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
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

    private void OnThemeChanged(ThemeChangedEventArgs e) => ApplyTheme();

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        RunOperation(() => _viewModel.CancelAsync());

    private void ViewScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (StepsGrid.SelectedItem is RunStepViewModel step)
        {
            RunOperation(() => _viewModel.LoadScreenshotAsync(step));
        }
    }

    private void CloseScreenshot_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CloseScreenshot();

    private void ApplyTheme()
    {
        SetBrush("Cliq.Run.Background", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.Run.ToolbarBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.Run.Foreground", EnvironmentColors.ToolWindowTextColorKey);
        SetBrush("Cliq.Run.MutedForeground", EnvironmentColors.ToolWindowTextColorKey, 0.72);
        SetBrush("Cliq.Run.Border", EnvironmentColors.ToolWindowBorderColorKey);
        SetBrush("Cliq.Run.ButtonBackground", EnvironmentColors.CommandBarGradientMiddleColorKey);
        SetBrush("Cliq.Run.Error", EnvironmentColors.SystemHighlightColorKey);
    }

    private void SetBrush(string key, ThemeResourceKey colorKey, double opacity = 1)
    {
        var color = VSColorTheme.GetThemedColor(colorKey);
        Resources[key] = new SolidColorBrush(
            Color.FromArgb(color.A, color.R, color.G, color.B))
        {
            Opacity = opacity,
        };
    }

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "The WPF event intentionally starts an observed joinable task.")]
    private static void RunOperation(Func<System.Threading.Tasks.Task> operation)
    {
        ThreadHelper.JoinableTaskFactory
            .RunAsync(operation)
            .FileAndForget("Cliq/RunResults");
    }
}

public sealed class ByteArrayToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null;
        }

        using (var stream = new MemoryStream(bytes, writable: false))
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
