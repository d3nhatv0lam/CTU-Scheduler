using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Update;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Features.Updater.Models;
using CTUScheduler.Presentation.Features.Updater.ViewModels;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Shared.Dialogs.ViewModels;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
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
    private readonly IUserInteractionService _userInteractionService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IUpdateService _updateService;
    private readonly ILogger<SplashScreenViewModel> _logger;


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
        IAppLifecycleService appLifetime,
        IUpdateService updateService,
        IUserInteractionService userInteractionService,
        IViewModelFactory viewModelFactory,
        ILogger<SplashScreenViewModel> logger)
    {
        _connectivityService = connectivityService;
        _userSettingService = userSettingService;
        _appLifetime = appLifetime;
        _updateService = updateService;
        _logger = logger;

        _userInteractionService = userInteractionService;
        _viewModelFactory = viewModelFactory;

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
                ShowMessage("Đang kiểm tra phiên bản",
                    delay: TimeSpan.FromSeconds(1)))
            .SelectMany(_ => Observable.FromAsync(CheckApplicationUpdates))
            .SelectMany(updateOutcome =>
            {
                if (updateOutcome == UpdateOutcome.Updated)
                {
                    return ShowMessage("Cập nhật ứng dụng thành công!\nĐang Khởi động lại...",
                        delay: TimeSpan.FromSeconds(5));
                }

                if (updateOutcome == UpdateOutcome.UpToDate)
                {
                    return ShowMessage("Kiểm tra phiên bản thành công!\nKhông có phiên bản mới",
                        delay: TimeSpan.FromSeconds(1));
                }

                if (updateOutcome == UpdateOutcome.Failed)
                {
                    return ShowMessage("Có lỗi xảy ra trong quá trình cập nhật!\nSử dụng phiên bản hiện tại",
                        delay: TimeSpan.FromSeconds(2));
                }

                if (updateOutcome is UpdateOutcome.Closed or UpdateOutcome.Ignored or UpdateOutcome.Cancelled)
                {
                    return ShowMessage("Cập nhật ứng dụng bị hủy bỏ\nSử dụng phiên bản hiện tại",
                        delay: TimeSpan.FromSeconds(2));
                }

                return Observable.Return(Unit.Default);
            })
            .SelectMany(_ =>
                ShowMessage("Đang kiểm tra dữ liệu",
                    TimeSpan.FromSeconds(1.5)))
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .SelectMany(_ => InitializeUserServices())
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
                    _logger.LogError(ex, "Error when running SpashScreen");
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

    private IObservable<Unit> InitializeUserServices()
    {
        return Observable.FromAsync(async ct => { await _userSettingService.InitializeAsync(ct); });
    }

    private async Task<UpdateOutcome> CheckApplicationUpdates(CancellationToken cancellationToken)
    {
        var checkUpdate = await _updateService.CheckForUpdatesAsync(cancellationToken);

        // if (!checkUpdate.IsUpdateAvailable)
        //     return UpdateOutcome.UpToDate;

        var updateViewModel = _viewModelFactory.Create<UpdateViewModel>();
        updateViewModel.Initialize(checkUpdate);

        var options = new WindowDialogOptions
        {
            ShowInTaskBar = true,
            Buttons = DialogButtons.None,
            SizeMode = DialogSizeMode.Content,
            IsCloseButtonVisible = false
        };

        var updateOutcome =
            await _userInteractionService.Dialog
                .ShowWindowDialog<UpdateViewModel, UpdateOutcome>(
                    updateViewModel, in options);

        return updateOutcome;
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