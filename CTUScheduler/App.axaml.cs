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
using CTUScheduler.Legacy.RegistrationInfor;
using CTUScheduler.Legacy.RuntimeCourseService;
using CTUScheduler.Legacy.ScheduleManager;
using CTUScheduler.Legacy.ScheduleProfileService;
using CTUScheduler.Legacy.User;
using CTUScheduler.Legacy.WebDriver;
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

public class App : Application
{
    public static readonly string AppVersion = "0.1";
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
        
        LogSessionHeader();
        
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
    private void LogSessionHeader()
    {
        string separator = new string('=', 60);
        Log.Information($"{separator}");
        Log.Information("    CTU-SCHEDULER LOGGER");
        Log.Information($"    Time: {DateTime.Now}");
        Log.Information($"{separator}");
    }
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();  // Xóa các provider mặc định (nếu có)
            logging.AddSerilog(dispose: false);      // Thêm Serilog
        });
        
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));
        services.AddSingleton<AppState>()
            .AddSingleton<IAppState>(sp => sp.GetRequiredService<AppState>());
        
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IWebDriverService,WebDriverService>();
        
        services.AddSingleton<ICtuSitePageFactory, CtuSitePageFactory>()
            .AddTransient<ILoginPage, LoginPage>()
            .AddTransient<ILoginService,LoginService>()
            .AddTransient<IMainPage, MainPage>()
            .AddTransient<IMainHomeService,MainHomeService>()
            .AddTransient<IRegistrationRulesPage, RegistrationRulesPage>()
            .AddTransient<IRegistrationRulesService,RegistrationRulesService>()
            .AddTransient<ICourseCatalogPage,CourseCatalogPage>()
            .AddTransient<ICourseCatalogService,CourseCatalogService>();
        
        services.AddSingleton<IUserSessionService, UserSessionService>()
            .AddSingleton<IWorkspaceStore, WorkspaceStore>();
        
        services.AddSingleton<ScheduleManager>()
            .AddSingleton<IScheduleManager>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IScheduleRegistrationService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<ICourseQueryService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IProfileQueryService>(sp => sp.GetRequiredService<ScheduleManager>());
        
        // cũ
        services.AddSingleton<ICTUWebDriverService, CtuWebDriverService>();
        // cũ
        services.AddSingleton<IScheduleProfileService, ScheduleProfileService>();
        services.AddSingleton<RuntimeCourseService>()
            .AddSingleton<IRuntimeCourseService>(sp => sp.GetRequiredService<RuntimeCourseService>())
            .AddSingleton<ICourseStateService>(sp => sp.GetRequiredService<RuntimeCourseService>());
        // cũ
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
        
        services.AddTransient<Func<ScheduleProfile, TimetableEditorViewModel>>(provider => 
            (profile) => ActivatorUtilities.CreateInstance<TimetableEditorViewModel>(provider, profile));
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

    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Exit -= Desktop_Exit;
            
            if (ServiceProvider is IAsyncDisposable asyncDisposable) 
                Task.Run(async () => await asyncDisposable.DisposeAsync()).Wait();
            else if (ServiceProvider is IDisposable disposableService)
                disposableService.Dispose();
            
            Log.Logger.Information("Application exiting normally.");
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error during application shutdown");
        }
        finally
        {
            Log.Logger.Information("App Exited!");
            Log.CloseAndFlush();
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
            args.SetObserved(); // Ngăn app bị crash nếu muốn
        };

        // Bắt lỗi của ReactiveUI
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex => 
        {
            Log.Error(ex, "ReactiveUI Exception");
            // Tại đây bạn có thể hiển thị Dialog báo lỗi cho User nếu muốn
        });
    }
}
