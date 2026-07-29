using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Authoring;
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
        Content = new WorkbenchControl(
            new WorkbenchViewModel(
                new DisconnectedWorkbenchStore(),
                new MessageBoxWorkbenchPrompt()));
    }
}

internal sealed class DisconnectedWorkbenchStore : IWorkbenchStore, IWorkspaceConfigurationStore
{
    private readonly Dictionary<string, TestCase> _tests =
        new Dictionary<string, TestCase>(StringComparer.Ordinal);
    private readonly HashSet<string> _secretKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private ProjectManifest? _project;
    private CliqSettings _settings = CreateDefaultSettings();

    public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        var project = _project ?? new ProjectManifest(
            ProjectManifest.CurrentSchemaVersion,
            "disconnected",
            "No Cliq workspace connected",
            "https://",
            BrowserKind.Chrome,
            new OutputSettings(),
            DateTimeOffset.UtcNow);
        return Task.FromResult(
            new WorkspaceSnapshot(
                string.Empty,
                project,
                Array.Empty<PageDefinition>(),
                new List<TestCase>(_tests.Values)));
    }

    public Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken)
    {
        _tests[test.Id] = test;
        return Task.FromResult(test);
    }

    public Task DeleteTestAsync(string testId, CancellationToken cancellationToken)
    {
        _tests.Remove(testId);
        return Task.CompletedTask;
    }

    public Task<CliqSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_settings);
    }

    public Task<IReadOnlyList<SecretDescriptor>> LoadSecretsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SecretDescriptor> descriptors = new List<SecretDescriptor>(
            System.Linq.Enumerable.Select(
                _secretKeys,
                key => new SecretDescriptor(key, true)));
        return Task.FromResult(descriptors);
    }

    public Task<ProjectManifest> SaveProjectAsync(
        ProjectManifest project,
        CancellationToken cancellationToken)
    {
        _project = project;
        return Task.FromResult(project);
    }

    public Task<CliqSettings> SaveSettingsAsync(
        CliqSettings settings,
        CancellationToken cancellationToken)
    {
        _settings = settings;
        return Task.FromResult(settings);
    }

    public Task SaveSecretAsync(
        string key,
        string plaintextValue,
        CancellationToken cancellationToken)
    {
        _secretKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string key, CancellationToken cancellationToken)
    {
        _secretKeys.Remove(key);
        return Task.CompletedTask;
    }

    private static CliqSettings CreateDefaultSettings()
    {
        return new CliqSettings(
            CliqSettings.CurrentSchemaVersion,
            new ElementIdentificationSettings(
                "data-testid",
                new[] { "testId", "id", "name" },
                Array.Empty<string>()),
            new TestExecutionSettings(10000, 30000, true),
            new ScanningSettings(30000, 1000, false));
    }
}

internal sealed class MessageBoxWorkbenchPrompt : IWorkbenchPrompt
{
    public Task<bool> ConfirmDiscardChangesAsync(
        string operation,
        CancellationToken cancellationToken)
    {
        var result = MessageBox.Show(
            $"You have unsaved Cliq changes. Discard them and {operation}?",
            "Cliq",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task<bool> ConfirmDeleteTestAsync(
        string testName,
        CancellationToken cancellationToken)
    {
        var result = MessageBox.Show(
            $"Delete test “{testName}”? This cannot be undone.",
            "Cliq",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return Task.FromResult(result == MessageBoxResult.Yes);
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
