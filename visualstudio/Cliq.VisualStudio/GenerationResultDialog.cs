using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Cliq.Domain;

namespace Cliq.VisualStudio;

internal sealed class GenerationResultDialog : CliqDialogWindow
{
    private readonly ListBox _artifacts;

    public GenerationResultDialog(GenerationResult result)
    {
        var artifacts = result.Artifacts;
        Title = "Cliq Generation Results";
        Width = 720;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var panel = new DockPanel { Margin = new Thickness(16) };
        var summary = new TextBlock
        {
            Text = $"{artifacts.Count} generated artifact{(artifacts.Count == 1 ? string.Empty : "s")}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
        };
        DockPanel.SetDock(summary, Dock.Top);
        panel.Children.Add(summary);

        if (result.Warnings.Count > 0)
        {
            var warning = new TextBlock
            {
                Text = string.Join(
                    Environment.NewLine,
                    result.Warnings.Select(item =>
                        $"{item.PageObjectName}: {item.RecoveredCount} of {item.ExpectedCount} custom methods need your decision - " +
                        $"this file was left untouched, not regenerated. Resolve via Pending Merges. ({item.Path})")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
            };
            AutomationProperties.SetName(warning, "Custom method recovery warnings");
            DockPanel.SetDock(warning, Dock.Top);
            panel.Children.Add(warning);
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0),
        };
        var open = new Button { Content = "_Open", MinWidth = 82, Margin = new Thickness(0, 0, 8, 0) };
        open.Click += (_, _) => OpenRequested?.Invoke(SelectedPath);
        var reveal = new Button { Content = "_Reveal", MinWidth = 82, Margin = new Thickness(0, 0, 8, 0) };
        reveal.Click += (_, _) => Reveal(SelectedPath);
        var close = new Button { Content = "Close", IsCancel = true, MinWidth = 82 };
        buttons.Children.Add(open);
        buttons.Children.Add(reveal);
        buttons.Children.Add(close);
        DockPanel.SetDock(buttons, Dock.Bottom);
        panel.Children.Add(buttons);

        _artifacts = new ListBox
        {
            ItemsSource = artifacts,
            DisplayMemberPath = nameof(GeneratedArtifact.Path),
        };
        _artifacts.MouseDoubleClick += (_, _) => OpenRequested?.Invoke(SelectedPath);
        if (artifacts.Count > 0)
        {
            _artifacts.SelectedIndex = 0;
        }
        panel.Children.Add(_artifacts);
        Content = panel;
    }

    public event Action<string?>? OpenRequested;

    private string? SelectedPath => (_artifacts.SelectedItem as GeneratedArtifact)?.Path;

    private static void Reveal(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = File.Exists(fullPath)
                    ? $"/select,\"{fullPath}\""
                    : $"\"{Path.GetDirectoryName(fullPath)}\"",
                UseShellExecute = true,
            });
    }
}
