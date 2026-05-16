using System;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using Microsoft.Extensions.DependencyInjection;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler.Presentation.Services.ApplicationStartup;

public sealed class AppStartup : IAppStartup
{
    private readonly IServiceProvider _sp;

    public AppStartup(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void Initialize(IApplicationLifetime lifetime)
    {
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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
        
    }
}
