using System;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.SplashScreen.Views;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.AppToplevel;
using CTUScheduler.Presentation.Services.Dialogs;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;

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
            .FromAssemblyOf<IViewModel>()
            .AddClasses(c => c.AssignableTo<ISingletonViewModel>())
            .AsSelf()
            .WithSingletonLifetime()

            .AddClasses(c => c.AssignableTo<IViewModel>()
                .Where(t => !typeof(ISingletonViewModel).IsAssignableFrom(t)))
            .AsSelf()
            .WithTransientLifetime()
        );

        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        
        // --- UI Helper Services ---
        services.AddSingleton<IToplevelService, ToplevelService>();
        services.AddSingleton<IViewportService, ViewportService>();
        services.AddSingleton<IDialogHostService, DialogHostService>();
        services.AddSingleton<ITimetableDialogService, TimetableDialogService>();

        // --- Windows & Initialization Logic ---
            
        // SplashScreenWindow: Cần khởi tạo ToplevelService
        services.AddTransient<SplashScreenWindow>(provider =>
        {
            var window = new SplashScreenWindow();
            // Inject window instance vào service để điều khiển
            provider.GetRequiredService<IToplevelService>().Initialize(window);
            return window;
        });

        // MainWindow: Cần khởi tạo ToplevelService & ViewportService
        services.AddTransient<MainWindow>(provider =>
        {
            var window = new MainWindow();
            provider.GetRequiredService<IToplevelService>().Initialize(window);
            provider.GetRequiredService<IViewportService>().Initialize(window);
            return window;
        });

        // --- ViewModel Factories ---
        // Factory để tạo ViewModel có tham số động (ScheduleProfile)
        services.AddTransient<Func<ScheduleProfile, TimetableEditorViewModel>>(provider => 
            (profile) => ActivatorUtilities.CreateInstance<TimetableEditorViewModel>(provider, profile));

        return services;
    }
}