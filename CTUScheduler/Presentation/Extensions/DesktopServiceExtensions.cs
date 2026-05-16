using System.Reflection;
using CTUScheduler.Presentation.Features.Authentication.ViewModels;
using CTUScheduler.Presentation.Features.Authentication.Views;
using CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Dialogs;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.ViewContext;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Services.ApplicationStartup;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shells.AppShell.ViewModels;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Extensions;

public static class DesktopServiceExtensions
{
    /// <summary>
    /// Đăng ký các Service thuộc về UI, View, Window, Dialog (Chỉ dành cho Desktop/Avalonia)
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // services.AddSingleton<IViewLocator, MsDiViewLocator>();

        // --- ViewModel Registration ---
        services.Scan(scan => scan
            .FromAssemblyOf<MainViewModel>()
            .AddClasses(c => c.AssignableTo<ISingletonViewModel>())
            .AsSelf()
            .WithSingletonLifetime()
            .AddClasses(c => c.AssignableTo<IViewModel>()
                .Where(t => !typeof(ISingletonViewModel).IsAssignableFrom(t)))
            .AsSelf()
            .WithTransientLifetime()
        );

        // scan all classes that implement IUiDisposable to dispose when the app is closed
        services.Scan(scan => scan
            .FromAssemblyOf<UrsaToast>()
            .AddClasses(c => c.AssignableTo<IUiDisposable>())
            .AsSelfWithInterfaces()
            .WithSingletonLifetime()
        );

        // đăng ký ReactiveUI view
        services.Scan(scan => scan
                .FromAssemblyOf<LoginView>()
                .AddClasses(c => c.AssignableTo(typeof(IViewFor<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime() // Có thể đổi thành singleton
        );

        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<INavigationRegionManager, NavigationRegionManager>();

        // --- UI Helper Services ---
        services.AddSingleton<IViewContextService, ViewContextService>()
            .AddSingleton<IDialogService, UrsaDialogService>()
            .AddSingleton<IUserInteractionService, UserInteractionService>()
            .AddSingleton<IViewportService, ViewportService>();

        // selection strategy
        services.AddTransient<ManualSchedulingStrategy>();
        services.AddTransient<QuickSchedulingStrategy>();

        // startup window
        services.AddSingleton<AppLifecycleManager>()
            .AddSingleton<IAppLifecycleController>(sp => sp.GetRequiredService<AppLifecycleManager>())
            .AddSingleton<IAppLifecycleService>(sp => sp.GetRequiredService<AppLifecycleManager>());
        services.AddSingleton<IAppStartup, AppStartup>();
        services.AddTransient<SplashScreenWindow>();
        services.AddTransient<MainWindow>();

        return services;
    }
}