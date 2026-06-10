using System;
using System.IO;
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
    private const double BaseDpi = 96.0;

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
        double? dpi = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            double logicalWidth =
                width ?? (control.Width > 0 && !double.IsNaN(control.Width) ? control.Width : VirtualWidth);
            double logicalHeight =
                height ?? (control.Height > 0 && !double.IsNaN(control.Height) ? control.Height : VirtualHeight);

            using var bitmap = await RenderCoreAsync(
                control, logicalWidth, logicalHeight, scale, dpi, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            bitmap.Save(targetStream);
        });
    }

    public async Task<Bitmap> RenderToBitmapAsync(
        Control control,
        double width,
        double height,
        double scale = 1.0,
        double? dpi = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await RenderCoreAsync(control, width, height, scale, dpi, cancellationToken);
        });
    }

    /// <summary>
    /// Logic render dùng chung cho cả 2 hàm public.
    /// Phải được gọi trên UI thread.
    /// </summary>
    /// <param name="dpi">
    /// null  → Physical Layout mode (96 DPI): control layout ở physicalSize = logicalSize × scale.
    ///         Dùng cho thumbnail/preview — vẽ lại chi tiết ở kích thước lớn hơn.<br/>
    /// value → High-DPI mode: control layout ở logicalSize, bitmap ở DPI chỉ định.
    ///         Dùng cho clipboard/export — sub-pixel anti-aliasing tốt hơn.
    /// </param>
    private async Task<RenderTargetBitmap> RenderCoreAsync(
        Control control,
        double logicalWidth,
        double logicalHeight,
        double scale,
        double? dpi,
        CancellationToken cancellationToken)
    {
        var topLevel = _viewContextService.CurrentTopLevel;
        if (topLevel == null)
            throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");

        var rootPanel = topLevel.FindDescendantOfType<Panel>();
        if (rootPanel == null)
            throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");

        // --- Tính toán kích thước và DPI theo chế độ ---
        double targetDpi;
        double controlWidth, controlHeight;
        double physicalWidth = logicalWidth * scale;
        double physicalHeight = logicalHeight * scale;

        if (dpi.HasValue)
        {
            // High-DPI mode: control layout ở logical size, bitmap ở DPI cao
            // → Avalonia render với nhiều pixel hơn mỗi logical unit (sub-pixel AA)
            targetDpi = dpi.Value;
            controlWidth = logicalWidth;
            controlHeight = logicalHeight;
        }
        else
        {
            // Physical Layout mode: control layout ở physical size, bitmap ở 96 DPI
            // → Nội dung được vẽ lại trực tiếp ở kích thước lớn hơn, không phải stretch
            targetDpi = BaseDpi;
            controlWidth = physicalWidth;
            controlHeight = physicalHeight;
        }

        var hiddenContainer = new Grid
        {
            Opacity = 0,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(-30000, -30000, 0, 0)
        };

        var oldWidth = control.Width;
        var oldHeight = control.Height;
        RenderTargetBitmap? bitmap = null;

        try
        {
            control.Width = controlWidth;
            control.Height = controlHeight;

            hiddenContainer.Children.Add(control);
            rootPanel.Children.Add(hiddenContainer);

            control.Measure(new Size(controlWidth, controlHeight));
            control.Arrange(new Rect(0, 0, controlWidth, controlHeight));
            control.UpdateLayout();

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            control.UpdateLayout();

            var pixelSize = new PixelSize(
                (int)Math.Ceiling(physicalWidth),
                (int)Math.Ceiling(physicalHeight));

            bitmap = new RenderTargetBitmap(pixelSize, new Vector(targetDpi, targetDpi));
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
    }
}