using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CTUScheduler.Presentation.Helpers;

public static class OffscreenRender
{
    public static RenderTargetBitmap CreateBitmapFromControl(Control control, double width, double height)
    {
        if (control == null) 
            throw new ArgumentNullException(nameof(control));

        // Lưu giá trị cũ để khôi phục (tùy chọn)
        var oldWidth = control.Width;
        var oldHeight = control.Height;
        var oldBackground = (control as TemplatedControl)?.Background;

        try
        {
            // 1. Ép kích thước rõ ràng
            control.Width = width;
            control.Height = height;

            // 2. Áp dụng template + style
            if (control is TemplatedControl templated)
            {
                templated.ApplyTemplate();
            }

            // 3. Background tạm thời (rất quan trọng để text render rõ, tránh mờ/trắng)
            if (control is TemplatedControl tc && tc.Background == null)
            {
                tc.Background = Brushes.White;   // hoặc màu nền bạn muốn
            }

            // 4. Ép Avalonia tính toán layout & binding đầy đủ
            control.Measure(new Size(width, height));
            control.Arrange(new Rect(0, 0, width, height));

            // Bước này cực kỳ quan trọng với template phức tạp
            control.UpdateLayout();

            // Một số trường hợp cần thêm lần nữa để binding/visual state chạy hết
            control.InvalidateVisual();
            control.UpdateLayout();

            // 5. Tạo bitmap và render
            var pixelSize = new PixelSize((int)Math.Ceiling(width), (int)Math.Ceiling(height));
            var dpi = new Vector(96, 96);

            var bitmap = new RenderTargetBitmap(pixelSize, dpi);
            bitmap.Render(control);

            return bitmap;
        }
        finally
        {
            // Khôi phục giá trị cũ (tốt cho reuse control)
            control.Width = oldWidth;
            control.Height = oldHeight;
            if (control is TemplatedControl tc)
                tc.Background = oldBackground;
        }
    }
}