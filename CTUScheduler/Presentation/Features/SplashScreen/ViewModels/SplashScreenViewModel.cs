using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Metadata;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Installation.ViewModels;
using CTUScheduler.Presentation.Features.Installation.Views;
using CTUScheduler.Presentation.Services.AppToplevel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels
{
    public class SplashScreenViewModel : ViewModelBase, IDisposable, IRequestClose
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IInternetStatusService _internetStatusService;
        private readonly IWebDriverService _webDriverService;

        private string _message = "Đang kiểm tra kết nối mạng";
        private string _errorMessage = string.Empty;
        private bool _isError = false;
        public event Action<object?>? RequestClose;

        public void Close(object? result = null)
        {
            RequestClose?.Invoke(null);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }
        public bool IsError
        {
            get => _isError;
            set => this.RaiseAndSetIfChanged(ref _isError, value);
        }

    public string Version => App.AppVersion;

        public ReactiveCommand<Unit,Unit> CloseAppCommand { get; }

        public SplashScreenViewModel()
        {
            _internetStatusService = App.ServiceProvider.GetRequiredService<IInternetStatusService>();
            _webDriverService = App.ServiceProvider.GetRequiredService<IWebDriverService>();
            var _toplevelService = App.ServiceProvider.GetRequiredService<IToplevelService>();
            CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);
            
            _webDriverService.InstallationStatus
                .Delay(TimeSpan.FromSeconds(1d), RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async message => Message = message)
                .DisposeWith(_disposables);

            {
                InstallationViewModel? vm = null;
                InstallationView? window = null;
                _webDriverService.IsInstalling
                    .Where(x => x)
                    .Take(1)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(async _ =>
                    {
                        vm = new(_webDriverService.InstallationProgress);
                        window = new InstallationView()
                        {
                            ViewModel = vm
                        };
                        _toplevelService.ShowWindow(window);
                    }).DisposeWith(_disposables);

                _webDriverService.IsInstalling
                    .Where(x => !x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        window?.Close();
                        vm?.Dispose();
                    }).DisposeWith(_disposables);
            }
            
            InitializeStartup();
        }

        /// <summary>
        /// 1. check internet connection
        /// 2. check webdriver installation
        /// </summary>
        private void InitializeStartup()
        {
            _internetStatusService.InternetStatusOnRefresh
                .Where(status => status)
                .Take(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => Message = "Mạng đã được kết nối!")
                .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
                .Do(_ => Message =  "Đang kiểm tra dịch vụ web..")
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectMany(async _ =>
                {
                    await _webDriverService.InitWebDriverService();
                    return Unit.Default;
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => Message = "dịch vụ web đã hoạt động!")
                .Delay(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => Message = "Đang khởi động ứng dụng...")
                .Delay(TimeSpan.FromSeconds(2d), RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                    {
                        Close();
                    }, 
                    ex =>
                    {
                        IsError = true;
                        ErrorMessage = "Lỗi Khi khởi động, Hãy mở lại app!, tự động tắt sau 30s";
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
            _disposables.Dispose();
        }
    }
}
