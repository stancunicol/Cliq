using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Cliq.VisualStudio;

public sealed class CliqComboBoxTemplateExtension : MarkupExtension
{
    public override object ProvideValue(System.IServiceProvider serviceProvider) =>
        CliqComboBoxTemplate.Create();
}

internal static class CliqComboBoxTemplate
{
    public static ControlTemplate Create()
    {
        var template = new ControlTemplate(typeof(ComboBox));
        var border = new FrameworkElementFactory(typeof(Border), "Border");
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

        var grid = new FrameworkElementFactory(typeof(Grid));
        border.AppendChild(grid);

        var selection = new FrameworkElementFactory(typeof(ContentPresenter));
        selection.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemProperty));
        selection.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemTemplateProperty));
        selection.SetValue(ContentPresenter.ContentTemplateSelectorProperty, new TemplateBindingExtension(ComboBox.ItemTemplateSelectorProperty));
        selection.SetValue(ContentPresenter.MarginProperty, new Thickness(7, 3, 28, 3));
        selection.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        selection.SetValue(FrameworkElement.IsHitTestVisibleProperty, false);
        grid.AppendChild(selection);

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "▾");
        arrow.SetValue(TextBlock.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        arrow.SetValue(FrameworkElement.IsHitTestVisibleProperty, false);
        grid.AppendChild(arrow);

        var toggle = new FrameworkElementFactory(typeof(ToggleButton));
        toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        toggle.SetValue(UIElement.OpacityProperty, 0d);
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(ButtonBase.ClickModeProperty, ClickMode.Press);
        toggle.SetBinding(
            ToggleButton.IsCheckedProperty,
            new Binding(nameof(ComboBox.IsDropDownOpen))
            {
                RelativeSource = RelativeSource.TemplatedParent,
                Mode = BindingMode.TwoWay,
            });
        grid.AppendChild(toggle);

        var popup = new FrameworkElementFactory(typeof(Popup), "PART_Popup");
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.StaysOpenProperty, false);
        popup.SetBinding(
            Popup.IsOpenProperty,
            new Binding(nameof(ComboBox.IsDropDownOpen))
            {
                RelativeSource = RelativeSource.TemplatedParent,
                Mode = BindingMode.TwoWay,
            });

        var popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        popupBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetBinding(
            FrameworkElement.MinWidthProperty,
            new Binding(nameof(FrameworkElement.ActualWidth))
            {
                RelativeSource = RelativeSource.TemplatedParent,
            });

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        var items = new FrameworkElementFactory(typeof(ItemsPresenter));
        scroll.AppendChild(items);
        popupBorder.AppendChild(scroll);
        popup.AppendChild(popupBorder);
        grid.AppendChild(popup);

        template.VisualTree = border;
        var disabled = new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false,
        };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55d, "Border"));
        template.Triggers.Add(disabled);
        return template;
    }
}
