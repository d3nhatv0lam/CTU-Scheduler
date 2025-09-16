using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using Splat;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.AppServices.Services.Viewport;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Features.SplashScreen.ViewModels;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Services.Adapter;
using CTUScheduler.Presentation.Services.AppToplevel;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using MainView = CTUScheduler.Presentation.Shells.AppShell.Views.MainView;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler;

public partial class App : Application
{
    public static string AppVersion { get; } = "0.1";
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // đăng ký ServiceCollection
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        // load view
        Locator.CurrentMutable.RegisterLazySingleton(() => new ConventionalViewLocator(), typeof(IViewLocator));

        // Load Web
        Task.Run(async () =>
        {
            try
            {
                var webService = ServiceProvider!.GetRequiredService<IWebDriverService>();
                await webService.InitWebDriverService();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize WebDriverService");
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop1)
                {
                    desktop1.Shutdown(1); // Shutdown the application with an error code
                }
            }
        });

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
                DataContext = new MainViewModel()
            };
            
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(
            path: "logs/log-.txt",             // File log tự động xoay theo ngày
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
        )
        .WriteTo.Debug() 
        .CreateLogger();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();  // Xóa các provider mặc định (nếu có)
            logging.AddSerilog();      // Thêm Serilog
        });
        services.AddSingleton<IInternetStatusService, InternetStatusService>(provider => new InternetStatusService(TimeSpan.FromSeconds(3)));
        services.AddSingleton<IWebDriverService,WebDriverService>();
        services.AddSingleton<ICTUWebDriverService, CTUWebDriverService>();
        services.AddSingleton<IUserDataService, UserDataService>();
        services.AddSingleton<ScheduleService>()
            .AddSingleton<IScheduleService, ScheduleService>(sp => sp.GetRequiredService<ScheduleService>())
            .AddSingleton<ICourseScheduleService, ScheduleService>(sp => sp.GetRequiredService<ScheduleService>());

        ConfigurePresentationServices(services);
        //services.AddSingleton<ICachingNavigationServiceFactory, CachingNavigationServiceFactory>();
    }
    
    private void ConfigurePresentationServices(IServiceCollection services)
    {
        services.AddSingleton<IToplevelService, ToplevelService>();
        services.AddSingleton<IViewportService, ViewportService>();
        services.AddSingleton<IDialogHostService, DialogHostService>();
        services.AddSingleton<ITimetableDialogService, TimetableDialogService>();
        services.AddSingleton<ITimetableLayoutAdapter, TimetableLayoutVmAdapter>();
        services.AddTransient<MainWindow>(provider =>
        {
            MainWindow window = new();
            provider.GetRequiredService<IToplevelService>().Initialize(window);
            provider.GetRequiredService<IViewportService>().Initialize(window);
            return window;
        });
    }
    
    private Window InitSplashScreenWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splashScreenViewModel = new SplashScreenViewModel();
        var splashScreen = new SplashScreenWindow()
        {
            DataContext = splashScreenViewModel
        };
        if (splashScreenViewModel is IRequestClose requestClose)
        {
            Action<object?>? handler = null;
            handler = (_) =>
            {
                requestClose.RequestClose -= handler;
                MainWindow mainWindow = App.ServiceProvider!.GetRequiredService<MainWindow>();
                mainWindow.DataContext = new MainViewModel();

                desktop.MainWindow = mainWindow;
                desktop.MainWindow?.Show();
                splashScreen.Close();
            };
            requestClose.RequestClose += handler;
        }
        return splashScreen;
    }

    private async void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
        if (ServiceProvider is IDisposable disposableService)
            disposableService.Dispose();

        await Log.CloseAndFlushAsync();

        if (sender is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit -= Desktop_Exit;
        }
    }
}
