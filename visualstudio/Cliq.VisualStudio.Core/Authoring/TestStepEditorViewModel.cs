using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cliq.Domain;

namespace Cliq.VisualStudio.Core.Authoring;

public sealed class TestStepEditorViewModel : BindableBase
{
    private readonly Func<IReadOnlyList<PageDefinition>> _pages;
    private string _id;
    private StepAction _action;
    private string _label;
    private bool _enabled;
    private string? _pageId;
    private string? _elementId;
    private string? _url;
    private string _valueKind;
    private string _value;
    private string _timeoutText;

    public TestStepEditorViewModel(
        TestStep step,
        Func<IReadOnlyList<PageDefinition>> pages)
    {
        _pages = pages ?? throw new ArgumentNullException(nameof(pages));
        _id = step.Id;
        _action = step.Action;
        _label = step.Label;
        _enabled = step.Enabled;
        _pageId = step.PageId;
        _elementId = step.ElementId;
        _url = step.Url;
        _valueKind = step.Value?.Kind ?? "literal";
        _value = step.Value?.Value ?? string.Empty;
        _timeoutText = step.TimeoutMs?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public event EventHandler? Changed;

    public string Id => _id;

    public StepAction Action
    {
        get => _action;
        set
        {
            if (!SetProperty(ref _action, value))
            {
                return;
            }

            NormalizeForAction();
            RaiseChanged();
            OnPropertyChanged(nameof(RequiresTarget));
            OnPropertyChanged(nameof(RequiresUrl));
            OnPropertyChanged(nameof(RequiresValue));
            OnPropertyChanged(nameof(LocatorDisplay));
            OnPropertyChanged(nameof(IsElementMissing));
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            if (SetProperty(ref _label, value ?? string.Empty))
            {
                RaiseChanged();
            }
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
            {
                RaiseChanged();
            }
        }
    }

    public string? PageId
    {
        get => _pageId;
        set
        {
            if (!SetProperty(ref _pageId, EmptyToNull(value)))
            {
                return;
            }

            if (SelectedPage?.Elements.Any(element => element.Id == _elementId) != true)
            {
                _elementId = SelectedPage?.Elements.FirstOrDefault()?.Id;
                OnPropertyChanged(nameof(ElementId));
            }

            RaiseChanged();
            OnPropertyChanged(nameof(AvailableElements));
            OnPropertyChanged(nameof(LocatorDisplay));
            OnPropertyChanged(nameof(IsElementMissing));
        }
    }

    public string? ElementId
    {
        get => _elementId;
        set
        {
            if (!SetProperty(ref _elementId, EmptyToNull(value)))
            {
                return;
            }

            RaiseChanged();
            OnPropertyChanged(nameof(LocatorDisplay));
            OnPropertyChanged(nameof(IsElementMissing));
        }
    }

    public string? Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, EmptyToNull(value)))
            {
                RaiseChanged();
            }
        }
    }

    public string ValueKind
    {
        get => _valueKind;
        set
        {
            var normalized = string.Equals(value, "secret", StringComparison.OrdinalIgnoreCase)
                ? "secret"
                : "literal";
            if (SetProperty(ref _valueKind, normalized))
            {
                RaiseChanged();
                OnPropertyChanged(nameof(IsSecretValue));
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value ?? string.Empty))
            {
                RaiseChanged();
            }
        }
    }

    public string TimeoutText
    {
        get => _timeoutText;
        set
        {
            if (SetProperty(ref _timeoutText, value?.Trim() ?? string.Empty))
            {
                RaiseChanged();
            }
        }
    }

    public bool RequiresTarget =>
        Action != StepAction.Navigate && Action != StepAction.AssertUrl;

    public bool RequiresUrl => Action == StepAction.Navigate;

    public bool RequiresValue =>
        Action == StepAction.Fill ||
        Action == StepAction.Select ||
        Action == StepAction.PressKey ||
        Action == StepAction.AssertText ||
        Action == StepAction.AssertUrl;

    public bool IsSecretValue => string.Equals(ValueKind, "secret", StringComparison.Ordinal);

    public IReadOnlyList<PageDefinition> AvailablePages => _pages();

    public IReadOnlyList<PageElement> AvailableElements =>
        SelectedPage?.Elements ?? Array.Empty<PageElement>();

    public string LocatorDisplay
    {
        get
        {
            var element = SelectedElement;
            return element == null
                ? "No locator selected"
                : $"{element.Locator.Strategy}: {element.Locator.Value}";
        }
    }

    public bool IsElementMissing => SelectedElement?.NotFound == true;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Label))
        {
            errors.Add("Step label is required.");
        }

        if (RequiresUrl &&
            (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            errors.Add("Navigate URL must be an absolute HTTP or HTTPS URL.");
        }

        if (RequiresTarget)
        {
            if (SelectedPage == null)
            {
                errors.Add("Select a page.");
            }
            else if (SelectedElement == null)
            {
                errors.Add("Select an element from the page.");
            }
        }

        if (RequiresValue && string.IsNullOrWhiteSpace(Value))
        {
            errors.Add(IsSecretValue ? "Secret key is required." : "Step value is required.");
        }

        if (!string.IsNullOrWhiteSpace(TimeoutText) &&
            (!int.TryParse(TimeoutText, NumberStyles.None, CultureInfo.InvariantCulture, out var timeout) ||
             timeout <= 0))
        {
            errors.Add("Timeout must be a positive whole number.");
        }

        return errors;
    }

    public TestStep ToModel()
    {
        int? timeout = null;
        if (int.TryParse(TimeoutText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            timeout = parsed;
        }

        return new TestStep(
            Id,
            Action,
            Label.Trim(),
            Enabled,
            RequiresTarget ? PageId : null,
            RequiresTarget ? ElementId : null,
            RequiresUrl ? Url?.Trim() : null,
            RequiresValue ? new StepValue(ValueKind, Value) : null,
            timeout);
    }

    public TestStepEditorViewModel Duplicate()
    {
        var duplicate = ToModel() with
        {
            Id = Guid.NewGuid().ToString("N"),
            Label = $"{Label} copy",
        };
        return new TestStepEditorViewModel(duplicate, _pages);
    }

    internal void NotifyPagesChanged()
    {
        OnPropertyChanged(nameof(AvailablePages));
        OnPropertyChanged(nameof(AvailableElements));
        OnPropertyChanged(nameof(LocatorDisplay));
        OnPropertyChanged(nameof(IsElementMissing));
    }

    private PageDefinition? SelectedPage =>
        _pages().FirstOrDefault(page => page.Id == PageId);

    private PageElement? SelectedElement =>
        SelectedPage?.Elements.FirstOrDefault(element => element.Id == ElementId);

    private void NormalizeForAction()
    {
        if (RequiresUrl)
        {
            _pageId = null;
            _elementId = null;
            _value = string.Empty;
            _valueKind = "literal";
            _url = string.IsNullOrWhiteSpace(_url)
                ? _pages().FirstOrDefault()?.Url ?? "https://"
                : _url;
        }
        else if (Action == StepAction.AssertUrl)
        {
            _pageId = null;
            _elementId = null;
            _url = null;
            if (string.IsNullOrWhiteSpace(_value))
            {
                _value = _pages().FirstOrDefault()?.Url ?? "https://";
            }
        }
        else
        {
            _url = null;
            var page = SelectedPage ?? _pages().FirstOrDefault();
            _pageId = page?.Id;
            _elementId = page?.Elements.FirstOrDefault()?.Id;
            if (!RequiresValue)
            {
                _value = string.Empty;
                _valueKind = "literal";
            }
        }

        OnPropertyChanged(nameof(PageId));
        OnPropertyChanged(nameof(ElementId));
        OnPropertyChanged(nameof(Url));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(ValueKind));
        OnPropertyChanged(nameof(IsSecretValue));
        OnPropertyChanged(nameof(AvailableElements));
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(ValidationSummary));
    }

    public string ValidationSummary => string.Join(Environment.NewLine, Validate());

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
