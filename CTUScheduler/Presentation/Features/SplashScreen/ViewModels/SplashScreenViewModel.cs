using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.SplashScreen.ViewModels
{
    public class SplashScreenViewModel : ViewModelBase , IDisposable, IRequestClose
    {
        private readonly IInternetStatusService _internetStatusService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private string _message = "Đang kiểm tra kết nối mạng..";
        
        public event Action<object?>? RequestClose;
        
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
                        .Subscribe(_ => RequestClose?.Invoke(null))
                        .DisposeWith(_disposables);
                    }
                    catch
                    {
                        RxApp.MainThreadScheduler.Schedule(() => Message = "Lỗi Khi khởi động, Hãy mở lại app!");
                    }
                }).DisposeWith(_disposables);
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
