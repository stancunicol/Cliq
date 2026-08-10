using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class SettingsEditorViewModel : BindableBase
{
    public static readonly IReadOnlyList<string> SupportedStrategies =
        new[] { "testId", "id", "name" };

    private string _testIdAttribute;
    private string _ignorePatternsText;
    private string _defaultStepTimeoutText;
    private string _pageReadyTimeoutText;
    private bool _captureScreenshotOnFailure;
    private string _loadTimeoutText;
    private string _networkIdleGraceText;
    private bool _captureScanScreenshot;
    private bool _isDirty;
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
        _loadTimeoutText = settings.Scanning.LoadTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _networkIdleGraceText = settings.Scanning.NetworkIdleGraceMs.ToString(CultureInfo.InvariantCulture);
        _captureScanScreenshot = settings.Scanning.CaptureScreenshot;
        _strategyToAdd = AvailableStrategies.FirstOrDefault();
    }

    public event EventHandler? DirtyChanged;

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

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                DirtyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string ValidationSummary => string.Join(Environment.NewLine, Validate());

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
                CaptureScreenshotOnFailure),
            new ScanningSettings(
                ParsePositiveInt(LoadTimeoutText),
                ParsePositiveInt(NetworkIdleGraceText),
                CaptureScanScreenshot));
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
        _loadTimeoutText = settings.Scanning.LoadTimeoutMs.ToString(CultureInfo.InvariantCulture);
        _networkIdleGraceText = settings.Scanning.NetworkIdleGraceMs.ToString(CultureInfo.InvariantCulture);
        _captureScanScreenshot = settings.Scanning.CaptureScreenshot;
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

    private void SetEditable<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            IsDirty = true;
            OnPropertyChanged(nameof(ValidationSummary));
        }
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
}
