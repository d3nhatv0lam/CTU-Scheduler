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
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Repositories;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.SplashScreen.Components.Installation.ViewModels;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels;

public partial class SplashScreenViewModel : ViewModelBase, IDisposable, IRequestClose
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IConnectivityService _connectivityService;
    private readonly IWebDriverService _webDriverServiceRefactor;
    private readonly IUserSettingService _userSettingService;
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
        IWebDriverService webDriverServiceRefactor,
        IWebDriverInstallerService webDriverInstallerService,
        IUserSettingService userSettingService,
        IAppLifecycleService appLifetime)
    {
        _connectivityService = connectivityService;
        _webDriverServiceRefactor = webDriverServiceRefactor;
        _userSettingService = userSettingService;
        
        _localCts = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);
        
        _installationViewModel = new InstallationViewModel(webDriverInstallerService.LogStream)
            .DisposeWith(_disposables);

        _isDownloadingHelper = webDriverInstallerService.IsBusy
            .ToProperty(this, nameof(IsDownloading), scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(_disposables);

        
        CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);
        ToggleConsoleCommand = ReactiveCommand.Create(ToggleConsole).DisposeWith(_disposables);

        webDriverInstallerService.StatusMessage
            .Skip(1)
            .Delay(TimeSpan.FromSeconds(1d), RxSchedulers.MainThreadScheduler)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(message => Message = message)
            .DisposeWith(_disposables);
        
        // clean up when download sussess
        this.WhenAnyValue(x => x.IsDownloading,
                x => x.IsExpanded,
                (isDownloading, isExpanded) => !isDownloading && isExpanded)
            .Skip(1)
            .Where(x => x)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
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
        _connectivityService.IsInternetAvailable
            .Where(status => status)
            .Take(1)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(_ => Message = "Mạng đã được kết nối!")
            // .Delay(TimeSpan.FromHours(1.5d), RxSchedulers.MainThreadScheduler)
            .Delay(TimeSpan.FromSeconds(1.5d), RxSchedulers.MainThreadScheduler)
            .Do(_ => Message = "Đang kiểm tra dịch vụ web..")
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .SelectMany(async _ =>
            {
                if (_localCts.IsCancellationRequested) return Unit.Default;
                
                try 
                {
                    await _userSettingService.InitializeAsync();
                    await _webDriverServiceRefactor.InitBrowserAsync();
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                
                return Unit.Default;
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(_ => Message = "dịch vụ web đã hoạt động!")
            .Delay(TimeSpan.FromSeconds(1.5d), RxSchedulers.MainThreadScheduler)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Do(_ => Message = "Đang khởi động ứng dụng...")
            .Delay(TimeSpan.FromSeconds(2d), RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => Close(),
                ex =>
                {
                    if (ex is OperationCanceledException) return;
                    Message = "Lỗi Khi khởi động, Hãy mở lại app!, tự động tắt sau 30s";
                    RxSchedulers.MainThreadScheduler.Schedule(TimeSpan.FromSeconds(30), CloseApplication);
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