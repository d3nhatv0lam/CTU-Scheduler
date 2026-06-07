using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public interface IControlRendererService
{
    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và lưu ra Stream đích dưới dạng ảnh Bitmap.
    /// <para>
    /// Có 2 chế độ render được chọn qua tham số <paramref name="dpi"/>:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Physical Layout</b> (<paramref name="dpi"/> = <c>null</c>, mặc định):
    ///     Control được layout ở kích thước vật lý (<c>logicalSize × scale</c>), bitmap ở 96 DPI.
    ///     Dùng cho ảnh thumbnail/preview — nội dung được vẽ lại ở kích thước lớn hơn.
    ///   </description></item>
    ///   <item><description>
    ///     <b>High-DPI</b> (<paramref name="dpi"/> được đặt, ví dụ <c>144</c>):
    ///     Control được layout ở kích thước logic, bitmap ở DPI chỉ định.
    ///     Dùng cho xuất ảnh chất lượng cao (clipboard, file) — sub-pixel anti-aliasing tốt hơn.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task RenderToStreamAsync(
        Control control,
        Stream targetStream,
        double? width = null,
        double? height = null,
        double scale = 1.0,
        double? dpi = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vẽ bất kỳ Control nào dưới nền (off-screen) và trả về đối tượng Bitmap trực tiếp (Zero-Copy).
    /// <para>
    /// Có 2 chế độ render được chọn qua tham số <paramref name="dpi"/>:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Physical Layout</b> (<paramref name="dpi"/> = <c>null</c>, mặc định):
    ///     Control được layout ở kích thước vật lý (<c>logicalSize × scale</c>), bitmap ở 96 DPI.
    ///     Dùng cho ảnh thumbnail/preview — nội dung được vẽ lại ở kích thước lớn hơn.
    ///   </description></item>
    ///   <item><description>
    ///     <b>High-DPI</b> (<paramref name="dpi"/> được đặt, ví dụ <c>144</c>):
    ///     Control được layout ở kích thước logic, bitmap ở DPI chỉ định.
    ///     Dùng cho xuất ảnh chất lượng cao (clipboard, file) — sub-pixel anti-aliasing tốt hơn.
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task<Bitmap> RenderToBitmapAsync(
        Control control,
        double width,
        double height,
        double scale = 1.0,
        double? dpi = null,
        CancellationToken cancellationToken = default);
}
