using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;

namespace Cliq.VisualStudio;

internal sealed class ScanResultDialog : CliqDialogWindow
{
    public ScanResultDialog(ScanPageResponse response)
    {
        var report = response.Reconciliation;
        Title = "Cliq Scan Results";
        Width = 640;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = $"Scanned {response.Page.Name}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Matched: {report.MatchedCount}    New: {report.NewCount}    Missing: {report.MissingCount}",
            Margin = new Thickness(0, 0, 0, 12),
        });

        var diff = response.LocatorDiff;
        if (diff is not null && (diff.New.Count > 0 || diff.Missing.Count > 0))
        {
            panel.Children.Add(SectionHeading("Locator changes"));
            if (diff.New.Count > 0)
            {
                panel.Children.Add(SubHeading($"New ({diff.New.Count})"));
                panel.Children.Add(EntryList(diff.New.Select(DescribeLocatorEntry)));
            }

            if (diff.Missing.Count > 0)
            {
                panel.Children.Add(SubHeading($"Missing ({diff.Missing.Count})"));
                panel.Children.Add(EntryList(diff.Missing.Select(DescribeLocatorEntry)));
            }
        }

        var suggestions = response.DynamicElementSuggestions;
        if (suggestions is { Count: > 0 })
        {
            panel.Children.Add(SectionHeading($"Dynamic element suggestions ({suggestions.Count})"));
            AutomationProperties.SetName(panel.Children[panel.Children.Count - 1], "Dynamic element suggestions");
            panel.Children.Add(EntryList(suggestions.Select(item =>
                $"{item.Name}: {item.Action} via {item.Strategy} ({item.SampleCount} similar elements found)")));
        }

        var close = new Button
        {
            Content = "Close",
            IsCancel = true,
            IsDefault = true,
            MinWidth = 82,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0),
        };
        panel.Children.Add(close);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel,
        };
    }

    private static string DescribeLocatorEntry(LocatorDiffEntry entry) =>
        $"{entry.Name} ({entry.Tag}) - {entry.Locator.Strategy}: {entry.Locator.Value}";

    private static TextBlock SectionHeading(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 4),
    };

    private static TextBlock SubHeading(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 4, 0, 2),
    };

    private static ItemsControl EntryList(IEnumerable<string> items) => new()
    {
        ItemsSource = items.ToArray(),
        Margin = new Thickness(8, 0, 0, 4),
    };
}
