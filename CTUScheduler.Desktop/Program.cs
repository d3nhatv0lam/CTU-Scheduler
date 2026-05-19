using System;
using System.Reactive;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.Desktop.Configs;
using CTUScheduler.Infrastructure.Extensions;
using CTUScheduler.Presentation.Extensions;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.ApplicationStartup;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Avalonia.Splat;
using Serilog;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Desktop;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
class Program
{
    private static IServiceProvider? _serviceProvider;

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

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.ForContext("ShortTypeName", "Host")
                .Fatal(ex, "Host terminated unexpectedly during start-up");
        }
        finally
        {
            DisposeServices();
            LoggingConfig.CloseAndFlush();
        }
    }
    
    // Avalonia configuration
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUIWithMicrosoftDependencyResolver(
                containerConfig: ConfigureServices,
                withResolver: sp =>
                {
                    _serviceProvider = sp;

                    var controller = sp?.GetRequiredService<IAppLifecycleController>();
                    controller?.NotifyStarted();
                }, 
                 withReactiveUIBuilder: rxui =>
                 {
                     rxui.WithExceptionHandler( Observer.Create<Exception>(ex => 
                     {
                         var rxLog = Log.ForContext("ShortTypeName", "UI");
                         rxLog.Error(ex, "ReactiveUI Pipeline/Command Exception");
                         // có thể hiển thị Dialog báo lỗi cho User tại đây
                     }));
                 })
            .AfterSetup(builder =>
            {
                if (builder.Instance is not App app)
                    return;

                var serviceProvider = _serviceProvider!;

                app.Startup = serviceProvider.GetRequiredService<IAppStartup>();
            });


    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: false));

        services.UseMicrosoftDependencyResolver();

        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddPresentationServices();
    }

    private static void DisposeServices()
    {
        var hostLog = Log.ForContext("ShortTypeName", "Host");
        if (_serviceProvider is null)
        {
            hostLog.Warning("Service provider is null. Skipping disposal.");
            return;
        }
        
        hostLog.Information("Cleaning up DI and infrastructure resources...");

        // Sử dụng Task.Run để giải phóng ThreadPool, ngăn chặn rủi ro Deadlock
        Task.Run(async () =>
        {
            try
            {
                if (_serviceProvider is IAsyncDisposable asyncDisposable)
                {
                    var disposeTask = asyncDisposable.DisposeAsync().AsTask();
                    await disposeTask.WaitAsync(TimeSpan.FromSeconds(15));
                    hostLog.Information("Shutdown complete successfully.");
                }
                else if (_serviceProvider is IDisposable disposable)
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
    }
}