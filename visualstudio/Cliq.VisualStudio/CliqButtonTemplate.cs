using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace Cliq.VisualStudio;

public sealed class CliqButtonTemplateExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => CliqButtonTemplate.Create();
}

/// <summary>
/// A rounded-corner Button chrome shared by every themed surface in the extension - XAML controls
/// via <see cref="CliqButtonTemplateExtension"/>, imperatively-built dialogs via
/// <see cref="CliqDialogWindow"/>. WPF's default Button chrome has no corner-radius property to
/// set, so getting rounded corners at all means replacing the whole template, not tweaking one.
/// Hover/pressed feedback is a semi-transparent white "state layer" overlay rather than a specific
/// accent color, since that reads correctly against either a plain button's themed background or
/// <c>PrimaryButton</c>'s accent background without the template needing to know which.
/// </summary>
internal static class CliqButtonTemplate
{
    /// <summary>Shared with <see cref="CliqInputTemplate"/>, <see cref="CliqComboBoxTemplate"/>,
    /// and the hand-built card borders in ScanReviewDialog.cs so every rounded corner in the
    /// extension matches.</summary>
    public const double DefaultCornerRadius = 7;

    public static ControlTemplate Create()
    {
        var template = new ControlTemplate(typeof(Button));

        var border = new FrameworkElementFactory(typeof(Border), "Border");
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(DefaultCornerRadius));
        border.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var grid = new FrameworkElementFactory(typeof(Grid));
        border.AppendChild(grid);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
        content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        content.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        grid.AppendChild(content);

        var stateLayer = new FrameworkElementFactory(typeof(Border), "StateLayer");
        stateLayer.SetValue(Border.BackgroundProperty, Brushes.White);
        stateLayer.SetValue(Border.CornerRadiusProperty, new CornerRadius(DefaultCornerRadius));
        stateLayer.SetValue(UIElement.OpacityProperty, 0d);
        stateLayer.SetValue(UIElement.IsHitTestVisibleProperty, false);
        grid.AppendChild(stateLayer);

        template.VisualTree = border;

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.08d, "StateLayer"));
        template.Triggers.Add(hover);

        var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.18d, "StateLayer"));
        template.Triggers.Add(pressed);

        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55d, "Border"));
        template.Triggers.Add(disabled);

        return template;
    }
}
