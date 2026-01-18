using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using System;
using System.Reactive;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler;

public class App : Application
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SetupGlobalExceptionHandling();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashScreen = InitSplashScreenWindow(desktop);
            desktop.MainWindow = splashScreen;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new SingleView()
            {
                DataContext = ServiceProvider.GetService<MainViewModel>()
            };
            
        }
        base.OnFrameworkInitializationCompleted();
    }
    
    private Window InitSplashScreenWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splashScreenViewModel = ServiceProvider.GetRequiredService<SplashScreenViewModel>();
        var splashScreen = ServiceProvider.GetRequiredService<SplashScreenWindow>();
        splashScreen.DataContext = splashScreenViewModel;
        if (splashScreenViewModel is IRequestClose requestClose)
        {
            Action<object?>? handler = null;
            handler = (_) =>
            {
                requestClose.RequestClose -= handler;
                MainWindow mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetService<MainViewModel>();

                desktop.MainWindow = mainWindow;
                desktop.MainWindow?.Show();
                splashScreen.Close();
            };
            requestClose.RequestClose += handler;
        }
        return splashScreen;
    }

  
    
    private void SetupGlobalExceptionHandling()
    {
        // Bắt lỗi ở các Thread phụ (Background threads)
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "APP CRASH: Unhandled Exception on Non-UI Thread");
        };

        // Bắt lỗi ở Task (Task bị lỗi mà không có await hoặc try-catch)
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Background Task Error (Unobserved)");
            args.SetObserved(); // Ngăn app bị crash
        };

        // Bắt lỗi của ReactiveUI
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
        {
            Log.Error(ex, "ReactiveUI Exception");
            // có thể hiển thị Dialog báo lỗi cho User tại đây
        });
    }
}
