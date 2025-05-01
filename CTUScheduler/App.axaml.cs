using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels;
using CTUScheduler.Presentation.ViewModels.Shells;
using CTUScheduler.Presentation.ViewModels.SplashScreen;
using CTUScheduler.Presentation.Views;
using CTUScheduler.Presentation.Views.Shells;
using CTUScheduler.Presentation.Views.SplashScreen;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CTUScheduler;

public partial class App : Application
{
    public static string AppVersion { get; } = "0.1";
    public static IServiceProvider? ServiceProvider { get; private set; }

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
        Task.Run(() => ServiceProvider!.GetRequiredService<IWebDriverService>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            
            desktop.MainWindow = new SplashScreenWindow
            {
                DataContext = new SplashScreenViewModel()
            };
            desktop.MainWindow.Show();
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
        services.AddSingleton<IInternetStatusService, InternetStatusService>(provider => new InternetStatusService(TimeSpan.FromSeconds(3)));
        services.AddSingleton<IWebDriverService,WebDriverService>();
        services.AddSingleton<IUserDataService, UserDataService>();
        services.AddSingleton<IDialogHostService, DialogHostService>();
        //services.AddSingleton<ICachingNavigationServiceFactory, CachingNavigationServiceFactory>();
        ServiceProvider = services.BuildServiceProvider();
    }

    private async void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
        if (ServiceProvider is IDisposable disposableService)
            disposableService.Dispose();   
    }
}
