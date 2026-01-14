using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
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
            // check sau
            var cleanShutdown = Task.Run(async () =>
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
                    Console.WriteLine($"Cleanup error: {ex.Message}");
                    Log.Error(ex, "Failed to clean up resources");
                    return false;
                }
            }).Wait(5000);

            if (!cleanShutdown)
            {
                Log.Warning("Shutdown timed out - some resources might be forced to close.");
            }
            else
            {
                Log.Information("Shutdown complete.");
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
