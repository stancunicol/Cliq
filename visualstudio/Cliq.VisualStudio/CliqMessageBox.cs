using System.Windows;

namespace Cliq.VisualStudio;

/// <summary>
/// Collapses the repeated "MessageBox.Show(message, "Cliq", MessageBoxButton.X, MessageBoxImage.Y)"
/// shape used throughout the package, dialogs, and tool windows into one call per intent, instead
/// of every call site spelling out the same button/icon combination by hand.
/// </summary>
internal static class CliqMessageBox
{
    public const string DefaultCaption = "Cliq";

    public static void ShowInfo(string message, string caption = DefaultCaption) =>
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void ShowWarning(string message, string caption = DefaultCaption) =>
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void ShowWarning(Window owner, string message, string caption = DefaultCaption) =>
        MessageBox.Show(owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void ShowError(string message, string caption = DefaultCaption) =>
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

    public static bool ConfirmYesNo(string message, string caption = DefaultCaption) =>
        MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
            == MessageBoxResult.Yes;

    public static bool ConfirmYesNo(Window owner, string message, string caption = DefaultCaption) =>
        MessageBox.Show(owner, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
            == MessageBoxResult.Yes;

    public static MessageBoxResult AskYesNoCancel(string message, MessageBoxResult defaultResult, string caption = DefaultCaption) =>
        MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, defaultResult);
}
