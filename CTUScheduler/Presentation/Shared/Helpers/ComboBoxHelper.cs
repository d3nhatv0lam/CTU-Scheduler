using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CTUScheduler.Presentation.Shared.Helpers;

public class ComboBoxHelper : AvaloniaObject
{
    public static readonly AttachedProperty<bool> DisableWheelChangeProperty =
        AvaloniaProperty.RegisterAttached<ComboBoxHelper, ComboBox, bool>(
            "DisableWheelChange",
            defaultValue: false);

    public static bool GetDisableWheelChange(ComboBox element) => element.GetValue(DisableWheelChangeProperty);
    public static void SetDisableWheelChange(ComboBox element, bool value) => element.SetValue(DisableWheelChangeProperty, value);

    static ComboBoxHelper()
    {
        DisableWheelChangeProperty.Changed.AddClassHandler<ComboBox>((comboBox, e) =>
        {
            if (e.NewValue is true)
            {
                comboBox.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
            }
            else
            {
                comboBox.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
            }
        });
    }

    private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Chặn đổi giá trị khi dropdown đang đóng, vẫn cho phép cuộn khi đang mở danh sách
        if (sender is ComboBox cb && !cb.IsDropDownOpen)
        {
            e.Handled = true;
        }
    }
}
