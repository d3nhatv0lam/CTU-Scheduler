using System;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.Auth;
using CTUScheduler.AppServices.Services.MainHomeService;
using CTUScheduler.AppServices.Services.Network;
using CTUScheduler.AppServices.Services.Registration;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.AppServices.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký các Service thuộc về Logic, Data, Core, Network
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // --- Utilities ---
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        // --- State Management ---
        services.AddSingleton<AppState>()
            .AddSingleton<IAppState>(sp => sp.GetRequiredService<AppState>());

        // --- Infrastructure / External Services ---
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IWebDriverService, WebDriverService>();
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddSingleton<IWorkspaceStore, WorkspaceStore>();

        // --- Page Factories & Page Services (CTU Site) ---
        services.AddSingleton<ICtuSitePageFactory, CtuSitePageFactory>()
            .AddTransient<ILoginPage, LoginPage>()
            .AddTransient<ILoginService, LoginService>()
            .AddTransient<IMainPage, MainPage>()
            .AddTransient<IMainHomeService, MainHomeService>()
            .AddTransient<IRegistrationRulesPage, RegistrationRulesPage>()
            .AddTransient<IRegistrationRulesService, RegistrationRulesService>()
            .AddTransient<ICourseCatalogPage, CourseCatalogPage>()
            .AddTransient<ICourseCatalogService, CourseCatalogService>();

        // --- Schedule Manager  ---
        // Đây là pattern quan trọng để đảm bảo tất cả interface đều trỏ về cùng 1 object ScheduleManager
        services.AddSingleton<ScheduleManager>()
            .AddSingleton<IScheduleManager>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IScheduleRegistrationService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<ICourseQueryService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IProfileQueryService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IScheduleSyncService>(sp => sp.GetRequiredService<ScheduleManager>());
        
        services.AddSingleton<ITimetableGeneratorService, TimetableGeneratorService>();

        return services;
    }
}