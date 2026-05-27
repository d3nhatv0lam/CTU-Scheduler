using System;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Models;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.TeachingPlanService;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Infrastructure.Services.Auth;
using CTUScheduler.Infrastructure.Services.MainHomeService;
using CTUScheduler.Infrastructure.Services.Registration;
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
        services.AddSingleton<UserSessionService>()
            .AddSingleton<IUserSessionService>(sp => sp.GetRequiredService<UserSessionService>())
            .AddSingleton<ICleanup>(sp => sp.GetRequiredService<UserSessionService>());

        // --- App Services / Use Cases ---
        services.AddTransient<ILoginService, LoginService>();
        services.AddTransient<IMainHomeService, MainHomeService>();
        services.AddTransient<IRegistrationRulesService, RegistrationRulesService>();
        services.AddTransient<ICourseCatalogService, CourseCatalogService>();
        services.AddTransient<ICourseRegistrationService, CourseRegistrationService>();
        services.AddTransient<ITuitionFeeService, TuitionFeeService>();
        services.AddTransient<ITeachingPlanLoaderService, TeachingPlanLoaderService>();

        services.AddSingleton<ITimetableGeneratorService, TimetableGeneratorService>();

        // stores
        services.AddSingleton<PlannedCourseStore>()
            .AddSingleton<IPlannedCourseStore>(sp => sp.GetRequiredService<PlannedCourseStore>())
            .AddSingleton<ICleanup>(sp => sp.GetRequiredService<PlannedCourseStore>());

        services.AddSingleton<TuitionFeeStore>()
            .AddSingleton<ITuitionFeeStore>(sp => sp.GetRequiredService<TuitionFeeStore>())
            .AddSingleton<ICleanup>(sp => sp.GetRequiredService<TuitionFeeStore>());

        services.AddSingleton<CtuSessionStore>()
            .AddSingleton<ICtuSessionStore>(sp => sp.GetRequiredService<CtuSessionStore>())
            .AddSingleton<ICtuSessionAccessor>(sp => sp.GetRequiredService<CtuSessionStore>())
            .AddSingleton<ICleanup>(sp => sp.GetRequiredService<CtuSessionStore>())
            .AddSingleton<ISessionHeartbeatService, SessionHeartbeatService>();

        services.AddSingleton<TeachingPlanStore>()
            .AddSingleton<ITeachingPlanStore>(sp => sp.GetRequiredService<TeachingPlanStore>())
            .AddSingleton<ICleanup>(sp => sp.GetRequiredService<TeachingPlanStore>());

        // --- Schedule Manager Pattern ---
        services.AddSingleton<ScheduleManager>()
            .AddSingleton<IScheduleManager>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IScheduleRegistrationService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<ICourseQueryService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IProfileQueryService>(sp => sp.GetRequiredService<ScheduleManager>())
            .AddSingleton<IScheduleSyncService>(sp => sp.GetRequiredService<ScheduleManager>());

        services.AddSingleton<ISessionCoordinator, SessionCoordinator>();


        return services;
    }
}