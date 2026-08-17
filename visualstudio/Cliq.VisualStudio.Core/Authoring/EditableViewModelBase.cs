using System;
using System.Runtime.CompilerServices;

namespace Cliq.VisualStudio.Core.Authoring;

/// <summary>
/// Shared dirty-tracking plumbing for the workbench's editor view-models (project, settings,
/// page, test) - each one previously reimplemented its own IsDirty/DirtyChanged pair, and two of
/// them (ProjectEditorViewModel, SettingsEditorViewModel) reimplemented an identical SetEditable
/// helper on top of it as well.
/// </summary>
public abstract class EditableViewModelBase : BindableBase
{
    private bool _isDirty;

    public event EventHandler? DirtyChanged;

    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (SetProperty(ref _isDirty, value))
            {
                DirtyChanged?.Invoke(this, EventArgs.Empty);
                OnDirtyChanged();
            }
        }
    }

    public abstract string ValidationSummary { get; }

    /// <summary>Extra property-change notifications a subclass needs whenever IsDirty actually
    /// flips (e.g. TestEditorViewModel's DirtyMarker) - called right after DirtyChanged fires.</summary>
    protected virtual void OnDirtyChanged()
    {
    }

    protected void MarkDirty() => IsDirty = true;

    /// <summary>The shape every "plain editable field" setter in these view-models needs: set the
    /// field, and if it actually changed, mark the view-model dirty and re-raise ValidationSummary.
    /// A setter with additional side effects (e.g. also re-raising DisplayName) still calls this
    /// and raises its own extra ones afterward, based on the bool it returns; a setter that must
    /// NOT re-raise ValidationSummary (a couple of TestEditorViewModel's fields intentionally
    /// don't) calls SetProperty + MarkDirty directly instead of this.</summary>
    protected bool SetEditable<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            MarkDirty();
            OnPropertyChanged(nameof(ValidationSummary));
            return true;
        }

        return false;
    }
}
