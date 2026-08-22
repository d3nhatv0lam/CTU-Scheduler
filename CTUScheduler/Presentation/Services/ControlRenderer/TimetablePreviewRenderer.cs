using System.Threading;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Views;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public class TimetablePreviewRenderer : ITimetablePreviewRenderer
{
    private readonly IControlRendererService _controlRendererService;
    
    private const int CachedViewWidth = 600;
    private const int CachedViewHeight = 375;
    private const double CachedScale = 1.2D; 

    public TimetablePreviewRenderer(IControlRendererService controlRendererService)
    {
        _controlRendererService = controlRendererService;
    }

    public async Task<Bitmap> RenderPreviewAsync(TimetableViewModel visualizerVM,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Chờ cho các ô môn học hoặc học phần chưa xếp lịch được nạp xong từ Rx stream (ObserveOn).
            int checkCount = 0;
            while (visualizerVM.ScheduleCells.Count == 0 &&
                   visualizerVM.UnscheduledCourses.Count == 0 &&
                   checkCount < 30) // Tối đa 300ms
            {
                await Task.Delay(10, cancellationToken);
                checkCount++;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var view = new TimetableView()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                DataContext = visualizerVM
            };
            
            try
            {
                // Bật chế độ preview: ẩn các TextBlock phụ (Nhóm, Tín chỉ, Phòng, Sĩ số, Giảng viên)
                // Style Selector ":is(UserControl).preview TextBlock.detail" sẽ xử lý việc ẩn
                view.Classes.Add("preview");

                return await _controlRendererService.RenderToBitmapAsync(
                    view,
                    width: CachedViewWidth,
                    height: CachedViewHeight,
                    scale: CachedScale,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                view.Classes.Remove("preview");
                view.DataContext = null;
            }
        });
    }
}