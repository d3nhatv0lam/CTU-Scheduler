using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public interface IControlRendererService
{
    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và lưu ra Stream đích dưới dạng ảnh Bitmap
    /// </summary>
    Task RenderToStreamAsync(Control control, Stream targetStream, double dpi = 96.0);
}
