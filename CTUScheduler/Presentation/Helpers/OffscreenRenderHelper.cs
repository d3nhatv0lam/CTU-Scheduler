using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace CTUScheduler.Presentation.Helpers;

public static class OffscreenRenderHelper
{
    public async static Task<Bitmap> RenderWithHiddenWindow(Control control, int width, int height, double dpi = 96)
    {
        control.VerticalAlignment = VerticalAlignment.Stretch;
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var host = new Grid
        {
            Width = width,
            Height = height
        };
        host.Children.Add(control);
        
        var hiddenWindow = new Window()
        {
            Content = host,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint(-10000, -10000), // Đẩy ra ngoài màn hình
            ShowInTaskbar = false,
            ShowActivated = false,
        };
    
        // Hiển thị nhưng không focus
        hiddenWindow.Show();
        
        // Chờ layout update một chút
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        
        hiddenWindow.UpdateLayout();
        
        var pixelSize = new PixelSize(width, height);
        var rtb = new RenderTargetBitmap(pixelSize, new Vector(dpi, dpi));
        rtb.Render(control);
        
        hiddenWindow.Close();
        control.DataContext = null;
    
        return rtb; // Window tự dispose khi using
    }
    
    public static async Task<bool> SaveToFile(Control control, string path, int width, int height)
    {
        var bitmap = await RenderWithHiddenWindow(control, width, height);
        using var stream = File.OpenWrite(path);
        bitmap.Save(stream);
        return true;
    }
}