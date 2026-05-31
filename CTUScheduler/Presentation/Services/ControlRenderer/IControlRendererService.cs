using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public interface IControlRendererService
{
    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và lưu ra Stream đích dưới dạng ảnh ảnh Bitmap
    /// </summary>
    Task RenderToStreamAsync(
        Control control, 
        Stream targetStream, 
        double? width = null, 
        double? height = null, 
        double? dpi = null);

    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và trả về đối tượng Bitmap trực tiếp (Zero-Copy)
    /// </summary>
    public Task<Bitmap> RenderToBitmapAsync(
        Control control,
        double width,
        double height,
        double? dpi = null);
}
