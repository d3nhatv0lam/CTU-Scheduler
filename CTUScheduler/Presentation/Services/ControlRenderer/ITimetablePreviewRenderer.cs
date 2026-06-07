using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public interface ITimetablePreviewRenderer
{
    /// <summary>
    /// Nhận vào ViewModel hiển thị lịch và render ra ảnh xem trước dạng Bitmap
    /// </summary>
    Task<Bitmap> RenderPreviewAsync(TimetableViewModel visualizerVM, CancellationToken cancellationToken = default);
}
