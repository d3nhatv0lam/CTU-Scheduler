using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CTUScheduler.Core.Utils;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Views;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.ControlRenderer;

public class TimetablePreviewRenderer : ITimetablePreviewRenderer
{
    private readonly IControlRendererService _controlRendererService;
    private readonly LifoSemaphore _semaphoreSlim = new(6);

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

        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (visualizerVM.ScheduleCells.Count == 0 && visualizerVM.UnscheduledCourses.Count == 0)
            {
                var cellsReady = visualizerVM.WhenAnyValue(x => x.ScheduleCells.Count)
                    .Where(count => count > 0);
                var unscheduledReady = visualizerVM.WhenAnyValue(x => x.UnscheduledCourses.Count)
                    .Where(count => count > 0);

                await cellsReady.Merge(unscheduledReady)
                    .Take(1)
                    .Timeout(TimeSpan.FromMilliseconds(500), Observable.Return(0))
                    .ToTask(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
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
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}