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
    private const double VirtualWidth = 1200;
    private const double VirtualHeight = 800;

    public ControlRendererService(IViewContextService viewContextService)
    {
        _viewContextService = viewContextService;
    }

    public async Task RenderToStreamAsync(Control control, Stream targetStream, double dpi = 96.0)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var topLevel = _viewContextService.CurrentTopLevel;

            if (topLevel == null)
            {
                throw new InvalidOperationException("Không tìm thấy TopLevel đang hoạt động hiện tại.");
            }
            
            var rootPanel = topLevel.FindDescendantOfType<Panel>();
            if (rootPanel == null)
            {
                throw new InvalidOperationException("Không tìm thấy Panel thích hợp trên giao diện hoạt động.");
            }

            // Tạo một container ẩn để chứa Control cần chụp
            var hiddenContainer = new Grid
            {
                Opacity = 0,
                Width = VirtualWidth,
                Height = VirtualHeight,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(-30000, -30000, 0, 0)
            };

            try
            {
                hiddenContainer.Children.Add(control);
                rootPanel.Children.Add(hiddenContainer);

                control.Measure(new Size(VirtualWidth, VirtualHeight));
                control.Arrange(new Rect(0, 0, VirtualWidth, VirtualHeight));
                
                await Task.Delay(1); // Chờ layout engine tính toán xong

                var pixelSize = new PixelSize(
                    (int)(VirtualWidth * (dpi / 96.0)),
                    (int)(VirtualHeight * (dpi / 96.0))
                );

                using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(dpi, dpi));
                bitmap.Render(control);
                bitmap.Save(targetStream);
            }
            finally
            {
                rootPanel.Children.Remove(hiddenContainer);
                hiddenContainer.Children.Clear();
            }
        });
    }
}
