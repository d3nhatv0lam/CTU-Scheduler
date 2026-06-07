using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
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
        double scale = 1.0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var topLevel = _viewContextService.CurrentTopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");
            }

            // Tự động tính toán DPI và DPI Scale dựa trên scale
            double targetDpi = scale * 96.0;

            var rootPanel = topLevel.FindDescendantOfType<Panel>();
            if (rootPanel == null)
            {
                throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");
            }

            // width và height được xem là kích thước logic (DIPs) thiết kế mong muốn
            double logicalWidth =
                width ?? (control.Width > 0 && !double.IsNaN(control.Width) ? control.Width : VirtualWidth);
            double logicalHeight = height ??
                                   (control.Height > 0 && !double.IsNaN(control.Height)
                                       ? control.Height
                                       : VirtualHeight);

            // Kích thước vật lý thực tế của bitmap (pixelSize) = logicalSize * scale
            double physicalWidth = logicalWidth * scale;
            double physicalHeight = logicalHeight * scale;

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
                control.Width = logicalWidth;
                control.Height = logicalHeight;

                hiddenContainer.Children.Add(control);
                rootPanel.Children.Add(hiddenContainer);

                // 1. Đo đạc và sắp xếp theo đúng kích thước logical ban đầu
                control.Measure(new Size(logicalWidth, logicalHeight));
                control.Arrange(new Rect(0, 0, logicalWidth, logicalHeight));
                control.UpdateLayout();

                // Nhường luồng cho đến khi hệ thống vẽ xong layout ẩn dưới nền
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Tạo bitmap có kích thước vật lý (Pixel) và DPI tự động tính toán
                var pixelSize = new PixelSize(
                    (int)Math.Ceiling(physicalWidth),
                    (int)Math.Ceiling(physicalHeight)
                );

                using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(targetDpi, targetDpi));
                bitmap.Render(control);
                cancellationToken.ThrowIfCancellationRequested();
                bitmap.Save(targetStream);
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
        double scale = 1.0,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RenderTargetBitmap? bitmap = null;
        
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var topLevel = _viewContextService.CurrentTopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");
            }

            // Tự động tính toán DPI dựa trên scale
            double targetDpi = scale * 96.0;

            var rootPanel = topLevel.FindDescendantOfType<Panel>();
            if (rootPanel == null)
            {
                throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");
            }

            // width và height là kích thước logic (DIPs)
            double logicalWidth = width;
            double logicalHeight = height;

            // Kích thước vật lý thực tế của bitmap (pixelSize) = logicalSize * scale
            double physicalWidth = logicalWidth * scale;
            double physicalHeight = logicalHeight * scale;

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
                control.Width = logicalWidth;
                control.Height = logicalHeight;

                hiddenContainer.Children.Add(control);
                rootPanel.Children.Add(hiddenContainer);

                // 1. Đo đạc và sắp xếp theo đúng kích thước logical ban đầu
                control.Measure(new Size(logicalWidth, logicalHeight));
                control.Arrange(new Rect(0, 0, logicalWidth, logicalHeight));
                control.UpdateLayout();

                await Dispatcher.UIThread.InvokeAsync(
                    () => { },
                    DispatcherPriority.Render);

                cancellationToken.ThrowIfCancellationRequested();

                // 2. Tạo bitmap có kích thước vật lý (Pixel) và DPI tự động tính toán
                var pixelSize = new PixelSize(
                    (int)Math.Ceiling(physicalWidth),
                    (int)Math.Ceiling(physicalHeight));

                bitmap = new RenderTargetBitmap(
                    pixelSize,
                    new Vector(targetDpi, targetDpi));

                bitmap.Render(control);

                return bitmap;
            }
            catch
            {
                bitmap?.Dispose();
                throw;
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