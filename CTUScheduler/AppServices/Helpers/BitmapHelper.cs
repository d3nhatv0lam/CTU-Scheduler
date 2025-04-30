using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Helpers
{
    public static class BitmapHelper
    {
        /// <summary>
        /// Tạo một IBitmap rỗng (1x1 pixel với nền trong suốt).
        /// Bạn có thể thay đổi kích thước nếu cần.
        /// </summary>
        public static Bitmap CreateEmptyBitmap(int width = 1, int height = 1)
        {
            // Thiết lập kích thước và DPI (96 DPI là giá trị mặc định)
            var pixelSize = new PixelSize(width, height);
            var dpi = new Vector(96, 96);

            // Tạo WriteableBitmap với định dạng BGRA8888 và Alpha pre-multiplied
            var writeableBitmap = new WriteableBitmap(pixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
            return writeableBitmap;
        }
    }
}
