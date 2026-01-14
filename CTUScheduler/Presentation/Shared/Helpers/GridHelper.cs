using System;
using Avalonia;
using Avalonia.Controls;

namespace CTUScheduler.Presentation.Shared.Helpers;

public class GridHelper : AvaloniaObject
{
    // --- 1. COLUMN DEFINITIONS ---
    
    public static readonly AttachedProperty<string> ColDefsProperty =
        AvaloniaProperty.RegisterAttached<GridHelper, Grid, string>(
            "ColDefs", 
            defaultValue: "");

    public static string GetColDefs(Grid element) => element.GetValue(ColDefsProperty);
    public static void SetColDefs(Grid element, string value) => element.SetValue(ColDefsProperty, value);

    // --- 2. ROW DEFINITIONS (Khuyến mãi thêm cái này cho đủ bộ) ---
    
    public static readonly AttachedProperty<string> RowDefsProperty =
        AvaloniaProperty.RegisterAttached<GridHelper, Grid, string>(
            "RowDefs", 
            defaultValue: "");

    public static string GetRowDefs(Grid element) => element.GetValue(RowDefsProperty);
    public static void SetRowDefs(Grid element, string value) => element.SetValue(RowDefsProperty, value);

    // --- 3. STATIC CONSTRUCTOR (Xử lý logic) ---
    
    static GridHelper()
    {
        // Xử lý Column
        ColDefsProperty.Changed.Subscribe(e =>
        {
            // Kiểm tra sender là Grid và giá trị mới là chuỗi hợp lệ
            if (e is { Sender: Grid grid, NewValue.Value: { } param })
            {
                try 
                {
                    // Parse chuỗi thành ColumnDefinitions
                    grid.ColumnDefinitions = ColumnDefinitions.Parse(param);
                }
                catch
                {
                    // Bỏ qua nếu chuỗi sai định dạng để tránh crash app
                }
            }
        });

        // Xử lý Row
        RowDefsProperty.Changed.Subscribe(e =>
        {
            if (e is { Sender: Grid grid, NewValue.Value: { } param })
            {
                try
                {
                    grid.RowDefinitions = RowDefinitions.Parse(param);
                }
                catch
                {
                    // Ignore error
                }
            }
        });
    }
}