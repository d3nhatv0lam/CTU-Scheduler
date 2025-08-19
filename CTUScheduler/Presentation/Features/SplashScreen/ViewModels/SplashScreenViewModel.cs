using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels
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
