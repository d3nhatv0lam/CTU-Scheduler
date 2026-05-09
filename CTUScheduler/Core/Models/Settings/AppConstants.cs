using System;
using System.Collections.Generic;
using System.IO;
using CTUScheduler.Core.Models.Contributors;
using CTUScheduler.Core.Shared;
using CTUScheduler.Core.Shared.Helpers;

namespace CTUScheduler.Core.Models.Settings;

public static class AppConstants
{
    // ====================================================
    // CÁC HẰNG SỐ CHUNG (ROOT LEVEL)
    // Những cái này dùng chung cho toàn app, không thuộc nhóm cụ thể nào
    // ====================================================
    public const string AppVersion = "0.1";
    public const string AppNameWindows = "CTUScheduler";
    public const string AppNameUnix = "ctu-scheduler";

    public const int DefaultMaxScheduleProfiles = 10;

    // ====================================================
    // NHÓM FILE (NESTED CLASS)
    // Chỉ chứa tên file, phần mở rộng
    // ====================================================
    public static class Files
    {
        public const string UserPreferences = "UserPreferences.json";
        public const string AppLog = "app_log.log";
    }

    // ====================================================
    // NHÓM PATHS (Đường dẫn vật lý trên máy)
    // Dùng static property để evaluate lúc runtime
    // ====================================================
    public static class Paths
    {
        public static string BaseRoamingPath =>
            StandardPathBuilder.GetRoamingPath(PublisherConstants.Name, AppNameWindows, AppNameUnix);

        public static string BaseAppContext => StandardPathBuilder.GetAppBaseDirectory();

        // public static string BaseLocalPath =>
        //     StandardPathBuilder.GetLocalCachePath(PublisherConstants.Name, AppNameWindows, AppNameUnix);

        public static string UserPreferencesFilePath => Path.Combine(BaseAppContext, Files.UserPreferences);

        public static string AppLogFilePath => Path.Combine(BaseRoamingPath, Files.AppLog);
    }

    // ====================================================
    // NHÓM URL (NESTED CLASS)
    // Chứa các đường link liên kết
    // ====================================================
    public static class Urls
    {
        public const string GithubRepo = "https://github.com/d3nhatv0lam/CTU-Scheduler";
        public const string CtuSignIn = "https://htql.ctu.edu.vn/";
    }

    public static class AppCredits
    {
        public static readonly IReadOnlyList<ContributorProfile> AllContributors = new List<ContributorProfile>
        {
            new ContributorProfile(
                Id: "d3n",
                Name: "Dương Minh Đức",
                Nickname: "d3nhatv0lam / RxAmethyst",
                Tier: ContributorTier.Founder,
                Bio: "Đụng là dứt",
                AvatarUrl: null,
                SocialLinks: new Dictionary<SocialPlatform, string>
                {
                    [SocialPlatform.Facebook] = "https://www.facebook.com/profile.php?id=100088452777261",
                    [SocialPlatform.YouTube] = "https://www.youtube.com/@ucduong9984",
                    [SocialPlatform.GitHub] = "https://github.com/d3nhatv0lam"
                },
                Roles: new List<ContributorRole>
                {
                    ContributorRole.Developer,
                    ContributorRole.IdeaProvider,
                    ContributorRole.Documenter,
                    ContributorRole.Tester
                },
                DisplayOrder: 1
            ),
            new ContributorProfile(
                Id: "loc",
                Name: "Nguyễn Phước Lộc",
                Tier: ContributorTier.Maintainer,
                Nickname: "LEXIPIT3268",
                Bio: "Đụng là cọc",
                AvatarUrl: null,
                SocialLinks: new Dictionary<SocialPlatform, string>
                {
                    [SocialPlatform.Facebook] = "https://www.facebook.com/lexipit3268",
                    [SocialPlatform.YouTube] = "https://www.youtube.com/@lexipit3268",
                    [SocialPlatform.GitHub] = "https://github.com/lexipit3268"
                },
                Roles: new List<ContributorRole>
                {
                    ContributorRole.Designer,
                    ContributorRole.Tester
                },
                DisplayOrder: 2
            ),
            new ContributorProfile(
                Id: "phuc",
                Name: "Trần Trọng Phúc",
                Tier: ContributorTier.Maintainer,
                Nickname: null,
                Bio: "Đụng là cút",
                AvatarUrl: null,
                SocialLinks: new Dictionary<SocialPlatform, string>
                {
                    [SocialPlatform.GitHub] = "https://github.com/phuctran1501"
                },
                Roles: new List<ContributorRole>
                {
                    ContributorRole.Designer,
                    ContributorRole.Tester
                },
                DisplayOrder: 3
            ),
            new ContributorProfile(
                Id: "phat",
                Name: "Nguyễn Ngọc Đức Phát",
                Tier: ContributorTier.Maintainer,
                Nickname: "Kimgion",
                Bio: "Đụng là tát",
                AvatarUrl: null,
                SocialLinks: new Dictionary<SocialPlatform, string>
                {
                    [SocialPlatform.GitHub] = "https://github.com/KimgionDev"
                },
                Roles: new List<ContributorRole>
                {
                    ContributorRole.Designer
                },
                DisplayOrder: 4
            ),
        };
    }
}