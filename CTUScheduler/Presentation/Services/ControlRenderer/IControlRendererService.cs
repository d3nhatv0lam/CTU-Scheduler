using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public interface IControlRendererService
{
    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và lưu ra Stream đích dưới dạng ảnh Bitmap với tỉ lệ độ phân giải tùy chọn
    /// </summary>
    Task RenderToStreamAsync(
        Control control, 
        Stream targetStream, 
        double? width = null, 
        double? height = null, 
        double scale = 1.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và trả về đối tượng Bitmap trực tiếp (Zero-Copy) với tỉ lệ độ phân giải tùy chọn
    /// </summary>
    Task<Bitmap> RenderToBitmapAsync(
        Control control,
        double width,
        double height,
        double scale = 1.0,
        CancellationToken cancellationToken = default);
}
