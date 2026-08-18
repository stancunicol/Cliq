using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Cliq.VisualStudio;

public sealed class CliqTextBoxTemplateExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => CliqInputTemplate.Create(typeof(TextBox));
}

/// <summary>
/// A rounded-corner chrome for TextBox and PasswordBox, matching <see cref="CliqButtonTemplate"/>'s
/// corner radius. Both controls need a "PART_ContentHost" element in their template - that's a
/// WPF/TextBoxBase framework requirement, not a naming convention - so the shape is otherwise
/// identical between them.
/// </summary>
internal static class CliqInputTemplate
{
    public static ControlTemplate Create(Type targetType)
    {
        var template = new ControlTemplate(targetType);

        var border = new FrameworkElementFactory(typeof(Border), "Border");
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(CliqButtonTemplate.DefaultCornerRadius));
        border.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost");
        scrollViewer.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        if (targetType == typeof(TextBox))
        {
            // TextBoxBase (not PasswordBox) owns these - forwarded via TemplateBinding rather than
            // hardcoded so a multi-line preview TextBox that sets VerticalScrollBarVisibility="Auto"
            // on itself (e.g. MergeControl's read-only source-text boxes) still scrolls correctly;
            // the default WPF TextBox template wires this the same way.
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, new TemplateBindingExtension(TextBoxBase.VerticalScrollBarVisibilityProperty));
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, new TemplateBindingExtension(TextBoxBase.HorizontalScrollBarVisibilityProperty));
        }
        else
        {
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
        }

        border.AppendChild(scrollViewer);
        template.VisualTree = border;

        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55d, "Border"));
        template.Triggers.Add(disabled);

        return template;
    }
}
