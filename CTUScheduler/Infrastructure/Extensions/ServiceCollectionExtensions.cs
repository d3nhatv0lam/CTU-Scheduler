using System;
using System.IO;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Infrastructure.Repositories;
using CTUScheduler.Infrastructure.Services.Network;
using CTUScheduler.Infrastructure.Services.TeachingPlan;
using CTUScheduler.Infrastructure.Services.Update;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Clients;
using CTUScheduler.Infrastructure.Sites.CTU.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        // --- CTU HTTP Configuration ---
        services.Configure<CtuHttpClientOptions>(_ => { });
        services.AddHttpClient(CtuHttpClientOptions.AnonymousClientName)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<CtuHttpClientOptions>>().Value;
                options.ApplyBrowserHeaders(client.DefaultRequestHeaders);
                client.Timeout = options.PingTimeout;
            });

        // --- Teaching Plan ---
        services.AddSingleton<ITeachingPlanPdfParser, TeachingPlanPdfParser>();
        services.AddHttpClient<ITeachingPlanPdfService, TeachingPlanPdfService>()
            .ConfigureCtuDefaults();
        services.AddHttpClient<HtqlHtmlAnnouncementService>()
            .ConfigureCtuDefaults();
        services.AddHttpClient<HtqlJsonAnnouncementService>()
            .ConfigureCtuDefaults();
        services.AddTransient<ISchoolAnnouncementService, AggregatedSchoolAnnouncementService>();


        services.AddTransient<CtuSessionRecoveryHandler>();
        services.AddTransient<CtuJwtAuthHandler>();
        services.AddTransient<CtuLegacyCookieHandler>();
        services.AddTransient<HttpDebugTracingHandler>();

        services.AddHttpClient<AuthClient>()
            .ConfigureCtuDefaults()
            .AddHttpMessageHandler<CtuLegacyCookieHandler>();

        services.AddHttpClient<LegacyAuthClient>()
            .ConfigureCtuDefaults()
            .AddHttpMessageHandler<CtuLegacyCookieHandler>();

        services.AddSingleton<IAuthClient, AuthClientOrchestrator>();

        services.AddHttpClient<CourseRegistrationClient>()
            .ConfigureCtuDefaults()
            .AddHttpMessageHandler<CtuSessionRecoveryHandler>()
            .AddHttpMessageHandler<CtuJwtAuthHandler>();

        services.AddTransient<IRegistrationRulesClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());
        services.AddTransient<ICourseCatalogClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());
        services.AddTransient<ICourseRegistrationClient>(sp => sp.GetRequiredService<CourseRegistrationClient>());

        // --- Academic Grades ---
        services.AddSingleton<IAcademicGradeHtmlParser, AcademicGradeHtmlParser>();
        services.AddHttpClient<IAcademicGradeClient, AcademicGradeClient>()
            .ConfigureCtuDefaults()
            .AddHttpMessageHandler<CtuSessionRecoveryHandler>()
            .AddHttpMessageHandler<CtuLegacyCookieHandler>();

        services.AddSingleton<UpdateManager>(sp =>
        {
            // const string localTestDir = @"D:\LocalReleases";
            // // Nếu trên máy tính có tồn tại thư mục D:\LocalReleases -> Dùng cho việc test
            // if (Directory.Exists(localTestDir))
            // {
            //     return new UpdateManager(localTestDir);
            // }
            // Mặc định (trên máy sinh viên hoặc GitHub Actions không có thư mục này) -> Dùng GitHub
            return new UpdateManager(new GithubSource(AppConstants.Urls.GithubRepo, string.Empty, false));
        });
        services.AddSingleton<IUpdateService, VelopackUpdateService>();

        return services;
    }
}
