using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.DriverCore;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Infrastructure.Repositories;
using CTUScheduler.Infrastructure.Services.Auth;
using CTUScheduler.Infrastructure.Services.MainHomeService;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.Registration;
using CTUScheduler.Infrastructure.Services.TeachingPlan;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // --- External Tools ---
        services.AddSingleton<IConnectivityService, ConnectivityService>();
        services.AddSingleton<IExcelExporterService, ExcelExporterService>();

        // --- File Storage & Preferences ---
        services.AddSingleton<IWorkspaceRepository, WorkspaceRepository>();
        services.AddSingleton<IWorkspaceStore, WorkspaceStore>();

        services.Configure<UserPreferencesOptions>(options =>
        {
            options.FilePath = AppConstants.Paths.UserPreferencesFilePath;
        }).AddSingleton<IUserPreferencesRepository, UserPreferencesRepository>();

        // --- Playwright / Browser Automation ---
        services.AddSingleton<IWebDriverInstallerService, PlaywrightInstallerService>();
        services.AddSingleton<IWebDriverService, PlaywrightService>();
        services.AddSingleton<ICtuPageFactory, CtuPageFactory>();

        // --- Teaching Plan ---
        services.AddHttpClient<ITeachingPlanPdfService, TeachingPlanPdfService>();
        services.AddHttpClient<ISchoolAnnouncementService, SchoolAnnouncementService>();


        services.AddTransient<CtuSessionRecoveryHandler>();
        services.AddTransient<CtuJwtAuthHandler>();
        services.AddTransient<CtuLegacyCookieHandler>();

        services.AddHttpClient<IAuthClient, AuthClient>()
            .AddHttpMessageHandler<CtuLegacyCookieHandler>();
        
        services.AddHttpClient<CourseRegistrationClient>()
            .AddHttpMessageHandler<CtuSessionRecoveryHandler>()
            .AddHttpMessageHandler<CtuJwtAuthHandler>();
        services.AddTransient<IRegistrationRulesClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());
        services.AddTransient<ICourseCatalogClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());
        services.AddTransient<ICourseRegistrationClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());
        

        return services;
    }
}