using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using ReactiveUI;
using Ursa.ReactiveUIExtension;

namespace CTUScheduler.Presentation.Features.SplashScreen.Views;

public partial class SplashScreenWindow : ReactiveUrsaWindow<SplashScreenViewModel>
{
    public SplashScreenWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, x => x.CloseAppCommand, x => x.CloseAppButton).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Message, x => x.MessageTextBlock.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, x => x.Version, x => x.AppVersionTextBlock.Text,
                version => $"Version: {version}").DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.WindowWidth, x => x.ViewModel!.WindowHeight)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(16))
                .DistinctUntilChanged()
                .Where(size => size is { Item1: > 0, Item2: > 0 })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(tuple => Observable.FromAsync(ct => AnimateToSizeAsync(new Size(tuple.Item1, tuple.Item2), ct)))
                .Switch()
                .Subscribe()
                .DisposeWith(disposables);

            Disposable.Create(ViewModel, vm => vm?.Dispose()).DisposeWith(disposables);
        });
    }

    /// <summary>
    ///  using DispatcherTimer & Pos, It Cause CPU usage
    /// </summary>
    /// <param name="targetSize"></param>
    /// <param name="token"></param>
    // private Task AnimateToSizeAsync(Size targetSize, CancellationToken token)
    // {
    //     if (token.IsCancellationRequested)
    //         return Task.CompletedTask;
    //
    //     var screen = Screens.ScreenFromVisual(this);
    //     if (screen == null) return Task.CompletedTask;
    //
    //     var screenRect = screen.WorkingArea;
    //     var scaling = this.RenderScaling;
    //
    //     // Tính tâm màn hình
    //     var screenCenterX = screenRect.X + screenRect.Width / 2.0;
    //     var screenCenterY = screenRect.Y + screenRect.Height / 2.0;
    //
    //     var startSize = this.ClientSize;
    //
    //     var tcs = new TaskCompletionSource();
    //     var duration = TimeSpan.FromMilliseconds(350);
    //     var startTime = DateTime.Now;
    //
    //     var timer = new DispatcherTimer(DispatcherPriority.Render)
    //     {
    //         Interval = TimeSpan.FromMilliseconds(10)
    //     };
    //
    //     timer.Tick += (s, e) =>
    //     {
    //         if (token.IsCancellationRequested)
    //         {
    //             timer.Stop();
    //             tcs.TrySetResult();
    //             return;
    //         }
    //
    //         var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
    //         var t = elapsed / duration.TotalMilliseconds;
    //
    //         if (t >= 1.0)
    //         {
    //             timer.Stop();
    //             t = 1.0;
    //         }
    //
    //         var easeT = 1 - Math.Pow(1 - t, 3);
    //
    //         var currentW = startSize.Width + (targetSize.Width - startSize.Width) * easeT;
    //         var currentH = startSize.Height + (targetSize.Height - startSize.Height) * easeT;
    //
    //         // Cập nhật UI
    //         this.Width = currentW;
    //         this.Height = currentH;
    //
    //         // Căn giữa
    //         var currentPixelW = currentW * scaling;
    //         var currentPixelH = currentH * scaling;
    //         var newX = screenCenterX - (currentPixelW / 2.0);
    //         var newY = screenCenterY - (currentPixelH / 2.0);
    //         this.Position = new PixelPoint((int)newX, (int)newY);
    //
    //         if (t >= 1.0)
    //         {
    //             tcs.TrySetResult();
    //         }
    //     };
    //
    //     timer.Start();
    //     return tcs.Task;
    // }

    private async Task AnimateToSizeAsync(Size targetSize, CancellationToken token = default)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        double s = this.RenderScaling;
        if (topLevel == null || Screens.ScreenFromVisual(this) is not { } screen) return;

        // 1. TÍNH TOÁN
        var startRect = new Rect(Position.ToPoint(1), ClientSize * s);
        var targetArea = screen.WorkingArea;
        var targetPixelSize = targetSize * s;
        var targetRect = new Rect(
            targetArea.X + (targetArea.Width - targetPixelSize.Width) / 2,
            targetArea.Y + (targetArea.Height - targetPixelSize.Height) / 2,
            targetPixelSize.Width, targetPixelSize.Height);

        // 2. UNION & PREPARE
        var unionRect = startRect.Union(targetRect);
        var relStart = (startRect.Position - unionRect.Position) / s;
        var relTarget = (targetRect.Position - unionRect.Position) / s;

        // Thiết lập Window bao trùm nhanh
        this.Position = new PixelPoint((int)unionRect.X, (int)unionRect.Y);
        this.Width = unionRect.Width / s;
        this.Height = unionRect.Height / s;

        // Khóa Transition để chuẩn bị
        var backup = MainContainer.Transitions;
        MainContainer.Transitions = null;
        MainContainer.HorizontalAlignment = HorizontalAlignment.Left;
        MainContainer.VerticalAlignment = VerticalAlignment.Top;
        MainContainer.Margin = new Thickness(relStart.X, relStart.Y, 0, 0);
        MainContainer.Width = startRect.Width / s;
        MainContainer.Height = startRect.Height / s;

        // Chỉ đợi 1 frame rất ngắn (khoảng 16ms)
        await WaitForNextFrame(topLevel);
        MainContainer.Transitions = backup;

        // 3. START ANIMATION (300ms)
        MainContainer.Margin = new Thickness(relTarget.X, relTarget.Y, 0, 0);
        MainContainer.Width = targetSize.Width;
        MainContainer.Height = targetSize.Height;

        // 4. SNAP (Rút ngắn thời gian đợi)
        // Đợi 300ms + một chút bù trừ sai số
        await Task.Delay(320, token);

        // Tối ưu Snap: Không dùng Opacity nếu không cần thiết để tránh bị "nháy"
        // Chỉ dùng khi bạn thấy glitch quá nặng
        MainContainer.Transitions = null;

        this.Position = new PixelPoint((int)targetRect.X, (int)targetRect.Y);
        this.Width = targetSize.Width;
        this.Height = targetSize.Height;

        MainContainer.Margin = new Thickness(0);
        MainContainer.Width = MainContainer.Height = double.NaN;
        MainContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
        MainContainer.VerticalAlignment = VerticalAlignment.Stretch;

        this.UpdateLayout();
        await WaitForNextFrame(topLevel);

        MainContainer.Transitions = backup;
    }

    private Task WaitForNextFrame(TopLevel topLevel)
    {
        var tcs = new TaskCompletionSource();
        topLevel.RequestAnimationFrame(_ => tcs.SetResult());
        return tcs.Task;
    }
}