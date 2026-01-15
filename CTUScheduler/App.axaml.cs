using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using Splat;
using System;
using System.Reactive;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Auth;
using CTUScheduler.AppServices.Services.MainHomeService;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.AppToplevel;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using MainView = CTUScheduler.Presentation.Shells.AppShell.Views.MainView;
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
            desktop.Exit += Desktop_Exit;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
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

    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Exit -= Desktop_Exit;
            
            Log.Information("Stopping services and releasing resources..."); 
            
            // if (ServiceProvider is IAsyncDisposable asyncDisposable) 
            //     Task.Run(async () => await asyncDisposable.DisposeAsync()).Wait();
            // else if (ServiceProvider is IDisposable disposableService)
            //     disposableService.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during resource cleanup");
        }
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
