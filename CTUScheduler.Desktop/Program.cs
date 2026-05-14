using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.Desktop.Configs;
using CTUScheduler.Infrastructure.Extensions;
using CTUScheduler.Presentation.Extensions;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Avalonia.Splat;
using Serilog;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using IApplicationLifetime = CTUScheduler.Presentation.Services.ApplicationLifetime.IApplicationLifetime;

namespace CTUScheduler.Desktop;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LoggingConfig.Init();
        
        // Bắt lỗi ở các Thread phụ (Background threads)
        AppDomain.CurrentDomain.UnhandledException += (_, appDomainArgs) =>
        {
            var ex = appDomainArgs.ExceptionObject as Exception;
            var logger = Log.ForContext("ShortTypeName", "Application");
            logger.Fatal(ex, "APP CRASH: Unhandled Exception on Non-UI Thread {IsTerminating}",
                appDomainArgs.IsTerminating);
        };
        
        // Bắt lỗi ở Task (Task bị lỗi mà không có await hoặc try-catch)
        TaskScheduler.UnobservedTaskException += (_, taskSchedulerArgs) =>
        {
            var logger = Log.ForContext("ShortTypeName", "Application");
            logger.Error(taskSchedulerArgs.Exception, "Background Task Error (Unobserved)");
            taskSchedulerArgs.SetObserved();
        };

        ServiceProvider? serviceProvider = null;

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
            
            App.ServiceProvider = serviceProvider;

            var appLifetime = serviceProvider.GetRequiredService<IApplicationLifetime>() as AppLifetimeManager;
            appLifetime?.NotifyStarted();

            BuildAvaloniaApp(serviceProvider, appLifetime!)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.ForContext("ShortTypeName", "Host")
                .Fatal(ex, "Host terminated unexpectedly during start-up");
        }
        finally
        {
            var hostLog = Log.ForContext("ShortTypeName", "Host");

            hostLog.Information("Cleaning up DI and infrastructure resources...");

            // Sử dụng Task.Run để giải phóng ThreadPool, ngăn chặn rủi ro Deadlock
            Task.Run(async () =>
            {
                try
                {
                    if (serviceProvider is IAsyncDisposable asyncDisposable)
                    {
                        var disposeTask = asyncDisposable.DisposeAsync().AsTask();
                        await disposeTask.WaitAsync(TimeSpan.FromSeconds(15));
                        hostLog.Information("Shutdown complete successfully.");
                    }
                    else if (serviceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                        hostLog.Information("Shutdown complete successfully.");
                    }
                }
                catch (TimeoutException)
                {
                    hostLog.Warning("Shutdown timed out (15s)! Một số service chạy quá lâu. Ép buộc tắt...");
                }
                catch (Exception ex)
                {
                    hostLog.Error(ex, "Lỗi khi dọn dẹp tài nguyên DI.");
                }
            }).GetAwaiter().GetResult();

            hostLog.Information("================= LOG END =================");
            LoggingConfig.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        // chế độ Design
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(rxui => {});
    }

    // Avalonia configuration
    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider, AppLifetimeManager appLifetime)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(rxui => {})
            .AfterSetup(builder =>
            {
                if (builder.Instance is App { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime desktop })
                {
                    appLifetime.ShutdownRequested += () => desktop.Shutdown();

                    desktop.Exit += (_, _) =>
                    {
                        var uiLog = Log.ForContext("ShortTypeName", "UI");
                        uiLog.Information("UI Phase: Starting disposal of UI services...");

                        var disposableUiServices = serviceProvider.GetServices<IUiDisposable>();
                        int count = 0;
                        foreach (var service in disposableUiServices)
                        {
                            service.Dispose();
                            count++;
                        }

                        uiLog.Information("UI Phase: Disposed {Count} UI services successfully.", count);
                        appLifetime.NotifyStopped();
                    };
                }
            });

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: false));

        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();

        services.AddSingleton<IApplicationLifetime, AppLifetimeManager>();
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddPresentationServices();
    }
}