using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Services.UserInteractionService;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Dialogs;
using CTUScheduler.Presentation.Services.UserInteractionService.Implementations.Ursa.Notifications;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.ViewContext;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using CTUScheduler.Presentation.Services.Viewport;
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
        services.AddSingleton<IViewLocator, ConventionalViewLocator>();
        
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

        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<INavigationRegionManager, NavigationRegionManager>();
        
        // --- UI Helper Services ---
        services.AddSingleton<IViewContextService, ViewContextService>()
            .AddSingleton<IToastService, UrsaInteractionToast>()
            .AddSingleton<INotificationService, UrsaInteractionNotification>()
            .AddSingleton<IDialogService, UrsaDialogService>()
            .AddSingleton<IUserInteractionService, UserInteractionService>()
            .AddSingleton<IViewportService, ViewportService>();
        
        services.AddSingleton<IDialogHostService, DialogHostService>();
        services.AddSingleton<ITimetableDialogService, TimetableDialogService>();
        

        // selection strategy
        services.AddTransient<ManualSchedulingStrategy>();
        services.AddTransient<QuickSchedulingStrategy>();
        
        
        // SplashScreenWindow: Cần khởi tạo ToplevelService
        services.AddTransient<SplashScreenWindow>(provider =>
        {
            var window = new SplashScreenWindow();
            return window;
        });
        
        // MainWindow: Cần khởi tạo ToplevelService & ViewportService
        services.AddTransient<MainWindow>(provider =>
        {
            var window = new MainWindow();
            return window;
        });
        return services;
    }
}