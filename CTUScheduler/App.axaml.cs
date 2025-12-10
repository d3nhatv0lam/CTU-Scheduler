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
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.RegistrationInfor;
using CTUScheduler.AppServices.Services.ScheduleManager;
using CTUScheduler.AppServices.Services.User;
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
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using MainView = CTUScheduler.Presentation.Shells.AppShell.Views.MainView;
using MainWindow = CTUScheduler.Presentation.Shells.AppShell.Views.MainWindow;

namespace CTUScheduler;

public partial class App : Application
{
    public static string AppVersion = "0.1";
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: "logs/log-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
            )
            .WriteTo.Debug()
            .CreateLogger();

        // 2. Kích hoạt bắt lỗi toàn cục ngay lập tức
        SetupGlobalExceptionHandling();
        

        // đăng ký ServiceCollection
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        // load view
        Locator.CurrentMutable.RegisterLazySingleton(() => new ConventionalViewLocator(), typeof(IViewLocator));

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
        services.AddLogging(logging =>
        {
            logging.ClearProviders();  // Xóa các provider mặc định (nếu có)
            logging.AddSerilog(dispose: true);      // Thêm Serilog
        });
        
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IWebDriverService,WebDriverService>();
        services.AddSingleton<ICTUWebDriverService, CTUWebDriverService>();
        services.AddSingleton<IRegistrationInformationService, RegistrationInformationService>();
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
        services.AddTransient<SplashScreenWindow>(provider =>
        {
            SplashScreenWindow window = new();
            provider.GetRequiredService<IToplevelService>().Initialize(window);
            return window;
        });
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
        var splashScreen = ServiceProvider.GetRequiredService<SplashScreenWindow>();
        splashScreen.DataContext = splashScreenViewModel;
        
        if (splashScreenViewModel is IRequestClose requestClose)
        {
            Action<object?>? handler = null;
            handler = (_) =>
            {
                requestClose.RequestClose -= handler;
                MainWindow mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
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
        try
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Exit -= Desktop_Exit;
            
            if (ServiceProvider is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (ServiceProvider is IDisposable disposableService)
                disposableService.Dispose();
            
            Log.Information("Application exiting normally.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application shutdown");
        }
        finally
        {
            Log.Logger.Information("App Exited!");
            await Log.CloseAndFlushAsync();
        }
    }
    
    private void SetupGlobalExceptionHandling()
    {
        // Bắt lỗi ở các Thread phụ (Background threads)
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "APP CRASH: Unhandled Exception on Non-UI Thread");
        };

        // 2. Bắt lỗi ở Task (Task bị lỗi mà không có await hoặc try-catch)
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "Background Task Error (Unobserved)");
            args.SetObserved(); // Ngăn app bị crash nếu muốn
        };

        // 3. Bắt lỗi của ReactiveUI (Rất quan trọng vì bạn đang dùng ReactiveUI)
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
        {
            Log.Error(ex, "ReactiveUI Exception");
            // Tại đây bạn có thể hiển thị Dialog báo lỗi cho User nếu muốn
        });
    }
}
