using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Dialogs;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.Desktop.Configs;
using CTUScheduler.Presentation.Extensions;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Serilog;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

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
        // hạ tầng
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(dispose: false) 
            .ConfigureServices((context, services) =>
            {
                // reactiveUI register 
                services.UseMicrosoftDependencyResolver();
                var resolver = Locator.CurrentMutable;
                resolver.InitializeSplat();
                resolver.InitializeReactiveUI();
                // app services
                services.AddApplicationServices();
                services.AddPresentationServices();
            })
            .Build();
        
        try
        {
            host.Start();
            
            App.ServiceProvider = host.Services;
            // UI
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            var shutdownTask = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    await host.StopAsync(cts.Token);
        
                    if (host is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else
                        host.Dispose();
    
                    return true; 
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to clean up resources");
                    return false;
                }
            });
            
            bool finishedInTime = shutdownTask.Wait(5000);

            if (!finishedInTime)
            {
                Log.Warning("Shutdown timed out - some resources might be forced to close.");
            }
            else
            {
                if (shutdownTask.Result) 
                {
                    Log.Information("Shutdown complete successfully.");
                }
                else
                {
                    Log.Error("Shutdown completed but failed internally (check cleanup error log).");
                }
            }
            
            Log.Information("================= LOG END =================");
            LoggingConfig.Flush();
        }
    }

    // Avalonia configuration
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
