using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Cliq.VisualStudio.Core.Runs;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

public partial class RunResultsControl : CliqThemedUserControl
{
    private readonly RunResultsViewModel _viewModel;

    public RunResultsControl(RunResultsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override string DiagnosticTag => "Cliq/RunResults";

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

    private void StepsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _viewModel.CloseScreenshot();

    protected override void ApplyTheme()
    {
        SetBrush("Cliq.Run.Background", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.Run.ToolbarBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.Run.Foreground", EnvironmentColors.ToolWindowTextColorKey);
        SetBrush("Cliq.Run.MutedForeground", EnvironmentColors.ToolWindowTextColorKey, 0.72);
        SetBrush("Cliq.Run.Border", EnvironmentColors.ToolWindowBorderColorKey);
        SetBrush("Cliq.Run.ButtonBackground", EnvironmentColors.CommandBarGradientMiddleColorKey);
        SetBrandBrush("Cliq.Run.Accent", 139, 92, 246);
        SetBrandBrush("Cliq.Run.SelectedForeground", 255, 255, 255);
        SetBrush("Cliq.Run.Error", EnvironmentColors.ToolWindowValidationErrorTextColorKey);
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
