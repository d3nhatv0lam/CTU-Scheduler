using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using CTUScheduler.AppServices.Extensions;
using CTUScheduler.Presentation.Extensions;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

[assembly: SupportedOSPlatform("browser")]
namespace CTUScheduler.Browser;


internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(); 
        });
        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();
        
        
        services.AddSingleton<IApplicationLifetime,BrowerApplicationLifetime>();
        
        services.AddApplicationServices();
        services.AddPresentationServices();

        
        
        var provider = services.BuildServiceProvider();
        App.ServiceProvider = provider;

        
        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseReactiveUI();
}