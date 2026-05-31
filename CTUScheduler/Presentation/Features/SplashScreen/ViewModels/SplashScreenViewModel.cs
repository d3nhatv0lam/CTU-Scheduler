using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels;

public partial class SplashScreenViewModel : ViewModelBase, IDisposable, IRequestClose
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IConnectivityService _connectivityService;
    private readonly IUserSettingService _userSettingService;
    private readonly IAppLifecycleService _appLifetime;
    private readonly CancellationTokenSource _localCts;

    private bool _isDisposed;

    // --- 1. CONSTANTS CHO VIỆC RESIZE ---
    private const double SMALL_WIDTH = 300;
    private const double SMALL_HEIGHT = 400;
    private const double LARGE_WIDTH = 850;
    private const double LARGE_HEIGHT = 500;

    [Reactive(SetModifier = AccessModifier.Private)]
    private string _message = "Đang kiểm tra kết nối mạng";

    [Reactive] private double _windowWidth = SMALL_WIDTH;
    [Reactive] private double _windowHeight = SMALL_HEIGHT;

    public string Version => AppConstants.AppVersion;

    public ReactiveCommand<Unit, Unit> CloseAppCommand { get; }

    public event Action<object?>? RequestClose;

    public void Close(object? result = null)
    {
        RequestClose?.Invoke(null);
    }

    public SplashScreenViewModel(
        IConnectivityService connectivityService,
        IUserSettingService userSettingService,
        IAppLifecycleService appLifetime)
    {
        _connectivityService = connectivityService;
        _userSettingService = userSettingService;
        _appLifetime = appLifetime;

        _localCts = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);

        CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);

        InitializeStartup();
    }

    /// <summary>
    /// 1. check internet connection
    /// 2. check webdriver installation
    /// </summary>
    private void InitializeStartup()
    {
        _connectivityService.IsInternetAvailable
            .Where(status => status)
            .Take(1)
            .SelectMany(_ =>
                ShowMessage("Mạng đã được kết nối!",
                    TimeSpan.FromSeconds(1.5)))
            .SelectMany(_ =>
                ShowMessage("Đang kiểm tra dữ liệu",
                    TimeSpan.FromSeconds(1.5)))
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .SelectMany(_ => InitializeServices())
            .SelectMany(_ =>
                ShowMessage("Kiểm tra dữ liệu thành công!",
                    TimeSpan.FromSeconds(1.5)))
            .SelectMany(_ =>
                ShowMessage("Đang khởi động ứng dụng...",
                    TimeSpan.FromSeconds(2)))
            .Subscribe(_ => Close(),
                ex =>
                {
                    if (ex is OperationCanceledException) return;
                    Message = "Lỗi Khi khởi động, Hãy mở lại app!, tự động tắt sau 30s";
                    RxSchedulers.MainThreadScheduler.Schedule(TimeSpan.FromSeconds(30), CloseApplication);
                })
            .DisposeWith(_disposables);
    }

    private IObservable<Unit> ShowMessage(
        string message,
        TimeSpan? delay = null)
    {
        return Observable.Return(Unit.Default)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(_ => Message = message)
            .Delay(delay ?? TimeSpan.Zero, RxSchedulers.MainThreadScheduler);
    }

    private IObservable<Unit> InitializeServices()
    {
        return Observable.FromAsync(async ct => { await _userSettingService.InitializeAsync(ct); });
    }

    private void CloseApplication()
    {
        _appLifetime.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        try
        {
            if (!_localCts.IsCancellationRequested)
            {
                _localCts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // Bỏ qua nếu nó đã lỡ bị dispose ở đâu đó khác
        }
        finally
        {
            _localCts.Dispose();
            _disposables.Dispose();
        }
    }
}