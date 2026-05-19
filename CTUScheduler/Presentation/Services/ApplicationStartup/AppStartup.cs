using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using Microsoft.Extensions.DependencyInjection;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler.Presentation.Services.ApplicationStartup;

public sealed class AppStartup : IAppStartup
{
    private readonly IServiceProvider _sp;
    private readonly IUiShutdownCoordinator _shutdown;
    private readonly IAppLifecycleController _controller;

    public AppStartup(
        IServiceProvider sp,
        IUiShutdownCoordinator shutdown,
        IAppLifecycleController controller)
    {
        _sp = sp;
        _shutdown = shutdown;
        _controller = controller;
    }

    public void Initialize(IApplicationLifetime lifetime)
    {
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            LinkDesktopLifetime(desktop);

            var splashScreenViewModel = _sp.GetRequiredService<SplashScreenViewModel>();
            var splashScreen = _sp.GetRequiredService<SplashScreenWindow>();
            splashScreen.DataContext = splashScreenViewModel;
            
            if (splashScreenViewModel is IRequestClose requestClose)
            {
                Action<object?>? handler = null;
                handler = (_) =>
                {
                    requestClose.RequestClose -= handler;
                    MainWindow mainWindow = _sp.GetRequiredService<MainWindow>();
                    mainWindow.DataContext = _sp.GetService<MainViewModel>();

                    desktop.MainWindow = mainWindow;
                    desktop.MainWindow.Show();

                    splashScreen.Close();
                };
                requestClose.RequestClose += handler;
            }

            desktop.MainWindow = splashScreen;
        }
        else if (lifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new SingleView()
            {
                DataContext = _sp.GetService<MainViewModel>()
            };
        }
    }

    private void LinkDesktopLifetime(IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Command flow: VM -> Controller -> AppStartup -> Avalonia
        _controller.ShutdownRequested += () =>
        {
            Dispatcher.UIThread.Post(() => desktop.Shutdown());
        };

        // Exit flow: Avalonia -> AppStartup -> Controller (State Notifications)
        desktop.Exit += (_, _) =>
        {
            _controller.NotifyStopping();
            _shutdown.DisposeUiServices();
            _controller.NotifyStopped();
        };
    }
}
