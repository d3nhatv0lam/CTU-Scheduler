using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace CTUScheduler.Presentation.Features.Scheduling.Helpers;

public static class PreviewRenderer
{
    public static async Task<Bitmap> RenderToBitmapAsync<TView, TViewModel>(
        TViewModel viewModel,
        int width = 650,
        int height = 390)
        where TView : ReactiveUserControl<TViewModel>, new()
        where TViewModel : class
    {
        var view = new TView
        {
            DataContext = viewModel,
            Width = width,
            Height = height,
            Background = Brushes.White,
            IsHitTestVisible = false
        };

        // Sử dụng Border làm container thay vì Window
        var container = new Border
        {
            Child = view,
            Width = width,
            Height = height,
            Background = Brushes.Transparent
        };

        container.UpdateLayout();
        container.Measure(new Size(width, height));
        container.Arrange(new Rect(0, 0, width, height));

        // Bước 4: Final render
        var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        bitmap.Render(view);

        return bitmap;
    }
}