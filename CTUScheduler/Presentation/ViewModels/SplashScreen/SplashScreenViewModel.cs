using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CTUScheduler.AppServices;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.Shells;
using CTUScheduler.Presentation.Views;
using CTUScheduler.Presentation.Views.Shells;
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

namespace CTUScheduler.Presentation.ViewModels.SplashScreen
{
    public class SplashScreenViewModel : ViewModelBase , IDisposable
    {
        private readonly IInternetStatusService _internetStatusService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private string _message = "Đang kiểm tra kết nối mạng..";
        
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }
        public string Version => App.AppVersion;

        public ReactiveCommand<Unit,Unit> CloseAppCommand { get; }

        public SplashScreenViewModel()
        {
            _internetStatusService = App.ServiceProvider!.GetRequiredService<IInternetStatusService>();
            CloseAppCommand = ReactiveCommand.Create(CloseApplication).DisposeWith(_disposables);

            _internetStatusService.InternetStatusOnRefresh
                .Where(internetStatus => internetStatus)
                .TakeUntil(internetStatus => internetStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(internetStatus =>
                {
                    try
                    {
                         Message = "Mạng đã được kết nối!";
                         Observable.Timer(TimeSpan.FromSeconds(1.5d),RxApp.MainThreadScheduler)
                        .Do(_ => Message = "Đang khởi động ứng dụng..")
                        .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(1)))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => RunMainWindow())
                        .DisposeWith(_disposables);
                    }
                    catch
                    {
                        RxApp.MainThreadScheduler.Schedule(() => Message = "Lỗi Khi khởi động, Hãy mở lại app!");
                    }
                }).DisposeWith(_disposables);
        }


        private void RunMainWindow()
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {    
                var loadingScreen = desktop.MainWindow;
                MainWindow mainWindow = App.ServiceProvider!.GetRequiredService<MainWindow>();
                mainWindow.DataContext = new MainViewModel();

                desktop.MainWindow = mainWindow;

                desktop.MainWindow.Show();
                loadingScreen!.Close();
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
            _disposables.Dispose();
        }
    }
}
