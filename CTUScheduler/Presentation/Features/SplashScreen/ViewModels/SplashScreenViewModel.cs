using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.SplashScreen.Components.Installation.ViewModels;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using IApplicationLifetime = CTUScheduler.Presentation.Services.ApplicationLifetime.IApplicationLifetime;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels;

public partial class SplashScreenViewModel : ViewModelBase, IDisposable, IRequestClose
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IConnectivityService _connectivityService;
    private readonly IWebDriverService _webDriverService;
    private readonly IApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _localCts = new();
    
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
    [Reactive] private bool _isExpanded = false;
    [ObservableAsProperty] private bool _isDownloading = false;

    [Reactive(SetModifier = AccessModifier.Private)]
    private InstallationViewModel _installationViewModel;
    
    public string Version => AppConstants.AppVersion;

    public ReactiveCommand<Unit, Unit> CloseAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleConsoleCommand { get; }

    public event Action<object?>? RequestClose;

    public void Close(object? result = null)
    {
        RequestClose?.Invoke(null);
    }


    public SplashScreenViewModel(
        IConnectivityService connectivityService,
        IWebDriverService webDriverService,
        IApplicationLifetime appLifetime)
    {
        _connectivityService = connectivityService;
        _webDriverService = webDriverService;
        _appLifetime = appLifetime;
        _installationViewModel = new InstallationViewModel(_webDriverService.InstallationProgress)
            .DisposeWith(_disposables);

        _isDownloadingHelper = _webDriverService.IsInstalling
            .ToProperty(this, nameof(IsDownloading), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposables);

        
        CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);
        ToggleConsoleCommand = ReactiveCommand.Create(ToggleConsole).DisposeWith(_disposables);

        _webDriverService.InstallationStatus
            .Delay(TimeSpan.FromSeconds(1d), RxApp.MainThreadScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(message => Message = message)
            .DisposeWith(_disposables);
        
        // clean up when download sussess
        this.WhenAnyValue(x => x.IsDownloading,
                x => x.IsExpanded,
                (isDownloading, isExpanded) => !isDownloading && isExpanded)
            .Skip(1)
            .Where(x => x)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToggleConsole())
            .DisposeWith(_disposables);
        
        InitializeStartup();
    }

    /// <summary>
    /// 1. check internet connection
    /// 2. check webdriver installation
    /// </summary>
    private void InitializeStartup()
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _appLifetime.ApplicationStopping,
            _localCts.Token
        );

        _connectivityService.IsInternetAvailable
            .Where(status => status)
            .Take(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "Mạng đã được kết nối!")
            // .Delay(TimeSpan.FromHours(1.5d), RxApp.MainThreadScheduler)
            .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
            .Do(_ => Message = "Đang kiểm tra dịch vụ web..")
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(async _ =>
            {
                if (_localCts.IsCancellationRequested) return Unit.Default;

                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    _appLifetime.ApplicationStopping,
                    _localCts.Token
                );

                try 
                {
                    await _webDriverService.InitWebDriverService(linkedTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                
                return Unit.Default;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "dịch vụ web đã hoạt động!")
            .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "Đang khởi động ứng dụng...")
            .Delay(TimeSpan.FromSeconds(2d), RxApp.MainThreadScheduler)
            .Subscribe(_ => Close(),
                ex =>
                {
                    if (ex is OperationCanceledException) return;
                    Message = "Lỗi Khi khởi động, Hãy mở lại app!, tự động tắt sau 30s";
                    RxApp.MainThreadScheduler.Schedule(TimeSpan.FromSeconds(30), CloseApplication);
                })
            .DisposeWith(_disposables);
    }

    private void ToggleConsole()
    {
        IsExpanded = !IsExpanded;
        if (!IsExpanded)
        {
            WindowHeight = SMALL_HEIGHT;
            WindowWidth = SMALL_WIDTH;
        }
        else
        {
            WindowHeight = LARGE_HEIGHT;
            WindowWidth = LARGE_WIDTH;
        }
    }

    private void CloseApplication()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
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