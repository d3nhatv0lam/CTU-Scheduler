using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
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
    
    private Task AnimateToSizeAsync(Size targetSize, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.CompletedTask;
        
        var screen = Screens.ScreenFromVisual(this);
        if (screen == null) return Task.CompletedTask;

        var screenRect = screen.WorkingArea;
        var scaling = this.RenderScaling;

        // Tính tâm màn hình
        var screenCenterX = screenRect.X + screenRect.Width / 2.0;
        var screenCenterY = screenRect.Y + screenRect.Height / 2.0;

        var startSize = this.ClientSize;

        var tcs = new TaskCompletionSource();
        var duration = TimeSpan.FromMilliseconds(350);
        var startTime = DateTime.Now;

        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };

        timer.Tick += (s, e) =>
        {
            if (token.IsCancellationRequested)
            {
                timer.Stop();
                tcs.TrySetResult();
                return;
            }

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var t = elapsed / duration.TotalMilliseconds;

            if (t >= 1.0)
            {
                timer.Stop();
                t = 1.0;
            }

            var easeT = 1 - Math.Pow(1 - t, 3);

            var currentW = startSize.Width + (targetSize.Width - startSize.Width) * easeT;
            var currentH = startSize.Height + (targetSize.Height - startSize.Height) * easeT;

            // Cập nhật UI
            this.Width = currentW;
            this.Height = currentH;

            // Căn giữa
            var currentPixelW = currentW * scaling;
            var currentPixelH = currentH * scaling;
            var newX = screenCenterX - (currentPixelW / 2.0);
            var newY = screenCenterY - (currentPixelH / 2.0);
            this.Position = new PixelPoint((int)newX, (int)newY);

            if (t >= 1.0)
            {
                tcs.TrySetResult();
            }
        };

        timer.Start();
        return tcs.Task;
    }
}