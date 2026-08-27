using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class SettingsEditorViewModel : EditableViewModelBase
{
    // Cliq.Domain.IdentificationStrategies.All is the single source of truth, shared with
    // WorkspaceRepository.Settings.cs's own validation - a hardcoded copy here once went stale
    // (still listing the pre-2026-08-03 "testId, id, name" priority order after the backend moved
    // to "testId, xpath" only) and silently blocked Save for every real settings.json, since
    // Cliq.Domain is already referenced by both sides.
    public static readonly IReadOnlyList<string> SupportedStrategies = IdentificationStrategies.All;

    private string _testIdAttribute;
    private string _ignorePatternsText;
    private string _defaultStepTimeoutText;
    private string _pageReadyTimeoutText;
    private bool _captureScreenshotOnFailure;
    private string _highlightPauseText;
    private string _loadTimeoutText;
    private string _networkIdleGraceText;
    private bool _captureScanScreenshot;
    private string _examplePageObjectFilePath;
    private string? _strategyToAdd;

    public SettingsEditorViewModel(CliqSettings settings)
    {
        SchemaVersion = settings.SchemaVersion;
        _testIdAttribute = settings.ElementIdentification.TestIdAttribute;
        StrategyOrder = new ObservableCollection<string>(
            settings.ElementIdentification.StrategyOrder);
        StrategyOrder.CollectionChanged += OnStrategyOrderChanged;
        _ignorePatternsText = string.Join(Environment.NewLine, settings.ElementIdentification.IgnoreIdPatterns);
        _defaultStepTimeoutText = settings.TestExecution.DefaultStepTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _pageReadyTimeoutText = settings.TestExecution.PageReadyTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _captureScreenshotOnFailure = settings.TestExecution.CaptureScreenshotOnFailure;
        _highlightPauseText = settings.TestExecution.HighlightPauseMs.ToString(CultureInfo.InvariantCulture);
        _loadTimeoutText = settings.Scanning.LoadTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _networkIdleGraceText = settings.Scanning.NetworkIdleGraceMs.ToString(CultureInfo.InvariantCulture);
        _captureScanScreenshot = settings.Scanning.CaptureScreenshot;
        _examplePageObjectFilePath = settings.ExamplePageObjectFilePath ?? string.Empty;
        _strategyToAdd = AvailableStrategies.FirstOrDefault();
    }

    public int SchemaVersion { get; }

    public ObservableCollection<string> StrategyOrder { get; }

    public IEnumerable<string> AvailableStrategies =>
        SupportedStrategies.Where(strategy =>
            !StrategyOrder.Contains(strategy, StringComparer.OrdinalIgnoreCase));

    public string? StrategyToAdd
    {
        get => _strategyToAdd;
        set => SetProperty(ref _strategyToAdd, value);
    }

    public string TestIdAttribute
    {
        get => _testIdAttribute;
        set => SetEditable(ref _testIdAttribute, value ?? string.Empty);
    }

    public string IgnorePatternsText
    {
        get => _ignorePatternsText;
        set => SetEditable(ref _ignorePatternsText, value ?? string.Empty);
    }

    public string DefaultStepTimeoutText
    {
        get => _defaultStepTimeoutText;
        set => SetEditable(ref _defaultStepTimeoutText, value ?? string.Empty);
    }

    public string PageReadyTimeoutText
    {
        get => _pageReadyTimeoutText;
        set => SetEditable(ref _pageReadyTimeoutText, value ?? string.Empty);
    }

    public bool CaptureScreenshotOnFailure
    {
        get => _captureScreenshotOnFailure;
        set => SetEditable(ref _captureScreenshotOnFailure, value);
    }

    // Unlike the other execution timeouts, 0 is a meaningful value here (no pause - see
    // SeleniumTestRunner.Pause), so this is validated as non-negative rather than positive.
    public string HighlightPauseText
    {
        get => _highlightPauseText;
        set => SetEditable(ref _highlightPauseText, value ?? string.Empty);
    }

    public string LoadTimeoutText
    {
        get => _loadTimeoutText;
        set => SetEditable(ref _loadTimeoutText, value ?? string.Empty);
    }

    public string NetworkIdleGraceText
    {
        get => _networkIdleGraceText;
        set => SetEditable(ref _networkIdleGraceText, value ?? string.Empty);
    }

    public bool CaptureScanScreenshot
    {
        get => _captureScanScreenshot;
        set => SetEditable(ref _captureScanScreenshot, value);
    }

    // Path (relative to the workspace root) to an existing page object .cs file the user already
    // has, see CliqSettings.ExamplePageObjectFilePath - when set, generation produces a shared
    // BasePage.cs styled after it and every page inherits from that. Blank means none configured;
    // no validation here beyond that, since an unreadable path just falls back to Cliq's own
    // default naming at generation time rather than failing.
    public string ExamplePageObjectFilePath
    {
        get => _examplePageObjectFilePath;
        set => SetEditable(ref _examplePageObjectFilePath, value ?? string.Empty);
    }

    public override string ValidationSummary => string.Join(Environment.NewLine, Validate());

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(TestIdAttribute))
        {
            errors.Add("Test ID attribute is required.");
        }

        if (StrategyOrder.Count == 0)
        {
            errors.Add("At least one identification strategy is required.");
        }

        if (StrategyOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count() != StrategyOrder.Count ||
            StrategyOrder.Any(strategy => !SupportedStrategies.Contains(strategy, StringComparer.OrdinalIgnoreCase)))
        {
            errors.Add("Identification strategies must be unique supported values.");
        }

        foreach (var pattern in ParsePatterns())
        {
            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException)
            {
                errors.Add($"Ignore pattern '{pattern}' is not a valid regular expression.");
            }
        }

        ValidatePositiveInt(DefaultStepTimeoutText, "Default step timeout", errors);
        ValidatePositiveInt(PageReadyTimeoutText, "Page ready timeout", errors);
        ValidateNonNegativeInt(HighlightPauseText, "Highlight pause", errors);
        ValidatePositiveInt(LoadTimeoutText, "Scan load timeout", errors);
        ValidatePositiveInt(NetworkIdleGraceText, "Network idle grace", errors);
        return errors;
    }

    public void AddSelectedStrategy()
    {
        if (StrategyToAdd == null ||
            StrategyOrder.Contains(StrategyToAdd, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        StrategyOrder.Add(StrategyToAdd);
        StrategyToAdd = AvailableStrategies.FirstOrDefault();
    }

    public void RemoveStrategy(string? strategy)
    {
        if (strategy != null)
        {
            StrategyOrder.Remove(strategy);
        }
    }

    public void MoveStrategy(string? strategy, int offset)
    {
        if (strategy == null)
        {
            return;
        }

        var oldIndex = StrategyOrder.IndexOf(strategy);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= StrategyOrder.Count)
        {
            return;
        }

        StrategyOrder.Move(oldIndex, newIndex);
    }

    public CliqSettings ToModel()
    {
        return new CliqSettings(
            SchemaVersion,
            new ElementIdentificationSettings(
                TestIdAttribute.Trim(),
                StrategyOrder.ToArray(),
                ParsePatterns()),
            new TestExecutionSettings(
                ParsePositiveInt(DefaultStepTimeoutText),
                ParsePositiveInt(PageReadyTimeoutText),
                CaptureScreenshotOnFailure,
                ParseNonNegativeInt(HighlightPauseText)),
            new ScanningSettings(
                ParsePositiveInt(LoadTimeoutText),
                ParsePositiveInt(NetworkIdleGraceText),
                CaptureScanScreenshot),
            string.IsNullOrWhiteSpace(ExamplePageObjectFilePath) ? null : ExamplePageObjectFilePath.Trim());
    }

    public void AcceptChanges(CliqSettings settings)
    {
        StrategyOrder.CollectionChanged -= OnStrategyOrderChanged;
        StrategyOrder.Clear();
        foreach (var strategy in settings.ElementIdentification.StrategyOrder)
        {
            StrategyOrder.Add(strategy);
        }

        StrategyOrder.CollectionChanged += OnStrategyOrderChanged;
        _testIdAttribute = settings.ElementIdentification.TestIdAttribute;
        _ignorePatternsText = string.Join(Environment.NewLine, settings.ElementIdentification.IgnoreIdPatterns);
        _defaultStepTimeoutText = settings.TestExecution.DefaultStepTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _pageReadyTimeoutText = settings.TestExecution.PageReadyTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _captureScreenshotOnFailure = settings.TestExecution.CaptureScreenshotOnFailure;
        _highlightPauseText = settings.TestExecution.HighlightPauseMs.ToString(CultureInfo.InvariantCulture);
        _loadTimeoutText = settings.Scanning.LoadTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _networkIdleGraceText = settings.Scanning.NetworkIdleGraceMs.ToString(CultureInfo.InvariantCulture);
        _captureScanScreenshot = settings.Scanning.CaptureScreenshot;
        _examplePageObjectFilePath = settings.ExamplePageObjectFilePath ?? string.Empty;
        StrategyToAdd = AvailableStrategies.FirstOrDefault();
        OnPropertyChanged(string.Empty);
        IsDirty = false;
    }

    private IReadOnlyList<string> ParsePatterns()
    {
        return IgnorePatternsText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(pattern => pattern.Trim())
            .Where(pattern => pattern.Length > 0)
            .ToArray();
    }

    private void OnStrategyOrderChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        IsDirty = true;
        StrategyToAdd = AvailableStrategies.FirstOrDefault();
        OnPropertyChanged(nameof(AvailableStrategies));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    private static void ValidatePositiveInt(
        string value,
        string label,
        ICollection<string> errors)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed <= 0)
        {
            errors.Add($"{label} must be a positive whole number.");
        }
    }

    private static int ParsePositiveInt(string value)
    {
        return int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static void ValidateNonNegativeInt(
        string value,
        string label,
        ICollection<string> errors)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            errors.Add($"{label} must be a non-negative whole number.");
        }
    }

    private static int ParseNonNegativeInt(string value)
    {
        return int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
