using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Views;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public class TimetablePreviewRenderer : ITimetablePreviewRenderer
{
    private readonly IControlRendererService _controlRendererService;

    private TimetableView? _cachedView;

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
            if (visualizerVM.HasItems)
            {
                int checkCount = 0;
                while (visualizerVM.ScheduleCells.Count == 0 &&
                       visualizerVM.UnscheduledCourses.Count == 0 &&
                       checkCount < 30) // Tối đa 300ms
                {
                    await Task.Delay(10, cancellationToken);
                    checkCount++;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            _cachedView ??= new TimetableView()
            {
                Width = 1200,
                Height = 750
            };

            _cachedView.DataContext = visualizerVM;

            try
            {
                return await _controlRendererService.RenderToBitmapAsync(
                    _cachedView,
                    width: 1200,
                    height: 750,
                    scale: 1.0,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _cachedView.DataContext = null;
            }
        });
    }
}