using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Metadata;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Installation.ViewModels;
using CTUScheduler.Presentation.Features.Installation.Views;
using CTUScheduler.Presentation.Services.AppToplevel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels;

public partial class SplashScreenViewModel : ViewModelBase, IDisposable, IRequestClose
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IConnectivityService _connectivityService;
    private readonly IWebDriverService _webDriverService;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CancellationTokenSource _localCts = new();

    [Reactive(SetModifier = AccessModifier.Private)]
    private string _message = "Đang kiểm tra kết nối mạng";
   
    public event Action<object?>? RequestClose;

    public void Close(object? result = null)
    {
        RequestClose?.Invoke(null);
    }
    
    public string Version => AppConstants.AppVersion;

    public ReactiveCommand<Unit, Unit> CloseAppCommand { get; }

    public SplashScreenViewModel()
    {
        _connectivityService = App.ServiceProvider.GetRequiredService<IConnectivityService>();
        _webDriverService = App.ServiceProvider.GetRequiredService<IWebDriverService>();
        _appLifetime = App.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        var toplevelService = App.ServiceProvider.GetRequiredService<IToplevelService>();
        CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);

        _webDriverService.InstallationStatus
            .Delay(TimeSpan.FromSeconds(1d), RxApp.MainThreadScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe( message => Message = message)
            .DisposeWith(_disposables);


        InstallationView? installationView = null;
        _webDriverService.IsInstalling
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isInstalling =>
            {
                if (isInstalling && installationView is null)
                {
                    InstallationViewModel vm = new(_webDriverService.InstallationProgress);
                    installationView = new InstallationView
                    {
                        ViewModel = vm
                    };
                    toplevelService.ShowWindow(installationView);
                }
                else if (!isInstalling && installationView is not null)
                {
                    installationView.Close();
                    (installationView.ViewModel as IDisposable)?.Dispose();
                    installationView = null;
                }
            })
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
            .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
            .Do(_ => Message = "Đang kiểm tra dịch vụ web..")
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(async _ =>
            {
                using (linkedCts) 
                {
                    await _webDriverService.InitWebDriverService(linkedCts.Token);
                }
                return Unit.Default;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "dịch vụ web đã hoạt động!")
            .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => Message = "Đang khởi động ứng dụng...")
            .Delay(TimeSpan.FromSeconds(2d), RxApp.MainThreadScheduler)
            .Subscribe(_ =>  Close(),
                ex =>
                {
                    if (ex is OperationCanceledException) return;
                    Message = "Lỗi Khi khởi động, Hãy mở lại app!, tự động tắt sau 30s";
                    RxApp.MainThreadScheduler.Schedule(TimeSpan.FromSeconds(30), CloseApplication);
                })
            .DisposeWith(_disposables);
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
        _localCts.Cancel(); 
        _localCts.Dispose();
        _disposables.Dispose();
    }
}