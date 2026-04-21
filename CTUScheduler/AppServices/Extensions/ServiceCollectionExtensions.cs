using System;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.AppServices.State;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Infrastructure.Services.Auth;
using CTUScheduler.Infrastructure.Services.MainHomeService;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.Registration;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
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
        services.AddSingleton<AppState>();
        services.AddSingleton<IUserSettingService, UserSettingService>();

        // --- Infrastructure / External Services ---
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddSingleton<IWorkspaceStore, WorkspaceStore>();
        services.AddSingleton<IExcelExporterService, ExcelExporterService>();

        // --- WebDriver Services ---
        services.AddSingleton<IWebDriverInstallerService, PlaywrightInstallerService>();
        services.AddSingleton<IWebDriverService, PlaywrightService>();

        // --- Page Factory ---
        services.AddSingleton<ICtuPageFactory, CtuPageFactory>();

        // --- Application Services ---
        services.AddTransient<ILoginService, LoginService>();
        services.AddTransient<IMainHomeService, MainHomeService>();
        services.AddTransient<IRegistrationRulesService, RegistrationRulesService>();
        services.AddTransient<ICourseCatalogService, CourseCatalogService>();

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