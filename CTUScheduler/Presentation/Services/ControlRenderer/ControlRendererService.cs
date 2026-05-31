using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public class ControlRendererService : IControlRendererService
{
    private readonly IViewContextService _viewContextService;
    private const double VirtualWidth = 1600;
    private const double VirtualHeight = 1000;

    public ControlRendererService(IViewContextService viewContextService)
    {
        _viewContextService = viewContextService;
    }

    public async Task RenderToStreamAsync(
        Control control,
        Stream targetStream,
        double? width = null,
        double? height = null,
        double? dpi = null)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var topLevel = _viewContextService.CurrentTopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");
            }

            double targetDpi = dpi ?? 96.0;

            var rootPanel = topLevel.FindDescendantOfType<Panel>();
            if (rootPanel == null)
            {
                throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");
            }

            double targetWidth = width ?? (control.Width > 0 && !double.IsNaN(control.Width) ? control.Width : VirtualWidth);
            double targetHeight = height ?? (control.Height > 0 && !double.IsNaN(control.Height) ? control.Height : VirtualHeight);

            // Tạo một container ẩn để chứa Control cần chụp
            var hiddenContainer = new Grid
            {
                Opacity = 0,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(-30000, -30000, 0, 0)
            };

            var oldWidth = control.Width;
            var oldHeight = control.Height;

            try
            {
                control.Width = targetWidth;
                control.Height = targetHeight;

                hiddenContainer.Children.Add(control);
                rootPanel.Children.Add(hiddenContainer);

                // 1. Đo đạc và sắp xếp theo kích thước đích
                control.Measure(new Size(targetWidth, targetHeight));
                control.Arrange(new Rect(0, 0, targetWidth, targetHeight));
                control.UpdateLayout();

                // Nhường luồng cho đến khi hệ thống vẽ xong layout ẩn dưới nền
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                // 2. Tạo bitmap có kích thước và DPI chỉ định
                var pixelSize = new PixelSize(
                    (int)Math.Ceiling(targetWidth),
                    (int)Math.Ceiling(targetHeight)
                );

                using (var bitmap = new RenderTargetBitmap(pixelSize, new Vector(targetDpi, targetDpi)))
                {
                    bitmap.Render(control);
                    bitmap.Save(targetStream);
                }
            }
            finally
            {
                rootPanel.Children.Remove(hiddenContainer);
                hiddenContainer.Children.Clear();
                control.Width = oldWidth;
                control.Height = oldHeight;
            }
        });
    }

    public async Task<Bitmap> RenderToBitmapAsync(
        Control control,
        double width,
        double height,
        double? dpi = null)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var topLevel = _viewContextService.CurrentTopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");
            }

            double targetDpi = dpi ?? 96.0;

            var rootPanel = topLevel.FindDescendantOfType<Panel>();
            if (rootPanel == null)
            {
                throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");
            }

            // Tạo một container ẩn để chứa Control cần chụp
            var hiddenContainer = new Grid
            {
                Opacity = 0,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(-30000, -30000, 0, 0)
            };

            var oldWidth = control.Width;
            var oldHeight = control.Height;

            try
            {
                control.Width = width;
                control.Height = height;

                hiddenContainer.Children.Add(control);
                rootPanel.Children.Add(hiddenContainer);

                // 1. Đo đạc và sắp xếp theo kích thước đích
                control.Measure(new Size(width, height));
                control.Arrange(new Rect(0, 0, width, height));
                control.UpdateLayout();

                await Dispatcher.UIThread.InvokeAsync(
                    () => { },
                    DispatcherPriority.Render);

                // 2. Tạo bitmap có kích thước và DPI chỉ định
                var pixelSize = new PixelSize(
                    (int)Math.Ceiling(width),
                    (int)Math.Ceiling(height));

                var bitmap = new RenderTargetBitmap(
                    pixelSize,
                    new Vector(targetDpi, targetDpi));

                bitmap.Render(control);

                return bitmap;
            }
            finally
            {
                rootPanel.Children.Remove(hiddenContainer);
                hiddenContainer.Children.Clear();
                control.Width = oldWidth;
                control.Height = oldHeight;
            }
        });
    }
}