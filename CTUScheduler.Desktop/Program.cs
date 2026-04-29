using System;
using System.Runtime.Versioning;
using System.Threading;
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
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.Information("Cleaning up DI and infrastructure resources...");

            // Sử dụng Task.Run để giải phóng ThreadPool, ngăn chặn rủi ro Deadlock
            Task.Run(async () =>
            {
                try
                {
                    if (serviceProvider is IAsyncDisposable asyncDisposable)
                    {
                        var disposeTask = asyncDisposable.DisposeAsync().AsTask();
                        await disposeTask.WaitAsync(TimeSpan.FromSeconds(15));
                        Log.Information("Shutdown complete successfully.");
                    }
                    else if (serviceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                        Log.Information("Shutdown complete successfully.");
                    }
                }
                catch (TimeoutException)
                {
                    Log.Warning("Shutdown timed out (15s)! Một số service chạy quá lâu. Ép buộc tắt...");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi dọn dẹp tài nguyên DI.");
                }
            }).GetAwaiter().GetResult();

            Log.Information("================= LOG END =================");
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
            .UseReactiveUI();
    }

    // Avalonia configuration
    public static AppBuilder BuildAvaloniaApp(ServiceProvider serviceProvider, AppLifetimeManager appLifetime)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .AfterSetup(builder =>
            {
                if (builder.Instance is App { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime desktop })
                {
                    appLifetime.ShutdownRequested += () => desktop.Shutdown();

                    desktop.Exit += (_, _) =>
                    {
                        Log.Information("UI Phase: Starting disposal of UI services...");

                        var disposableUiServices = serviceProvider.GetServices<IUiDisposable>();
                        int count = 0;
                        foreach (var service in disposableUiServices)
                        {
                            service.Dispose();
                            count++;
                        }

                        Log.Information("UI Phase: Disposed {Count} UI services successfully.", count);
                        appLifetime.NotifyStopped();
                    };
                }
            });

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: false));

        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeReactiveUI();

        services.AddSingleton<IApplicationLifetime, AppLifetimeManager>();
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddPresentationServices();
    }
}