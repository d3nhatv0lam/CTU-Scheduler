using System;

using Avalonia;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.AppServices.Helpers;
using CTUScheduler.Presentation.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI.Avalonia.Splat;
using Serilog;

namespace CTUScheduler.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        LoggingConfig.Init();
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Log.Information("Application stopped cleanly (User closed app).");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.Information("================= LOG END =================");
            LoggingConfig.Flush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUIWithMicrosoftDependencyResolver(services =>
                {
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddSerilog(dispose: false);
                    });
                    services.AddApplicationServices();
                    services.AddPresentationServices();
                },
            provider =>
            {
                App.ServiceProvider = provider!;
            });
}
