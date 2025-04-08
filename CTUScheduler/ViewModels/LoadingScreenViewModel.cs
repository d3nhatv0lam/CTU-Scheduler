using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CTUScheduler.Services;
using CTUScheduler.Utilities;
using CTUScheduler.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CTUScheduler.ViewModels
{
    public class LoadingScreenViewModel : ViewModelBase , IDisposable
    {
        private readonly InternetStatusService _internetStatusService;
        private readonly WebDriverService _webDriverService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private string _message = "Đang kiểm tra kết nối mạng..";
        
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }
        public string Version => App.AppVersion;

        public ReactiveCommand<Unit,Unit> CloseAppCommand { get; }

        public LoadingScreenViewModel()
        {
            _internetStatusService = App.ServiceProvider!.GetRequiredService<InternetStatusService>();
#if DEBUG
            _webDriverService = App.ServiceProvider!.GetRequiredService<WebDriverService>();
#endif
            CloseAppCommand = ReactiveCommand.Create(() => CloseApplication());

            _internetStatusService.InternetStatusOnRefesh
                .Where(internetStatus => internetStatus)
                .TakeUntil(internetStatus => internetStatus)
                .Subscribe(async internetStatus =>
                {
                    try
                    {
                        RxApp.MainThreadScheduler.Schedule(() => Message = "Mạng đã được kết nối!");
                        await GoToSignPage();
                        Observable.Timer(TimeSpan.FromSeconds(1.5d), RxApp.MainThreadScheduler)
                        .Do(_ => Message = "Đang khởi động ứng dụng..")
                        .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(1)))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => RunMainWindow())
                        .DisposeWith(_disposables);
                        
                    }
                    catch
                    {
                        RxApp.MainThreadScheduler.Schedule(() => Message = "Lỗi khi kết nối Internet, Hãy mở lại app!");
                        Dispose();
                    }
                }).DisposeWith(_disposables);
        }

        private async Task GoToSignPage()
        {
            await _webDriverService.GoToPage(AppConstants.CTU_LOGIN_URL);
        }

        private void RunMainWindow()
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {    
                var loadingScreen = desktop.MainWindow;
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = new MainViewModel()
                };
                desktop.MainWindow.Show();
                loadingScreen!.Close();
                Dispose();
            }
        }

        private void CloseApplication()
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Dispose();
                desktop.Shutdown();
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
