using System;
using System.IO;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Infrastructure.Repositories;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.TeachingPlan;
using CTUScheduler.Infrastructure.Services.Update;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using Microsoft.Extensions.DependencyInjection;
using Velopack;
using Velopack.Sources;

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

        // --- Teaching Plan ---
        services.AddSingleton<ITeachingPlanPdfParser, TeachingPlanPdfParser>();
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

// Khi cần test local, chỉ cần trỏ vào 1 thư mục trên máy (ví dụ: D:\TestReleases)
#if DEBUG
        var testReleasesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CTUScheduler");
        services.AddSingleton(sp => Directory.Exists(testReleasesDir)
            ? new UpdateManager(testReleasesDir) // Đọc từ thư mục máy tính
            : new UpdateManager(new GithubSource(AppConstants.Urls.GithubRepo, string.Empty, false)));
#else
    services.AddSingleton(sp => new UpdateManager(new GithubSource(AppConstants.Urls.GithubRepo, string.Empty, false)));
#endif
        services.AddSingleton<IUpdateService, VelopackUpdateService>();

        return services;
    }
}