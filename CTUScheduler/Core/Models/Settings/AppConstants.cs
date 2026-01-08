using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Settings;

public static class AppConstants
{
    // ====================================================
    // CÁC HẰNG SỐ CHUNG (ROOT LEVEL)
    // Những cái này dùng chung cho toàn app, không thuộc nhóm cụ thể nào
    // ====================================================
    public const string AppVersion = "0.1";
    public const int DefaultMaxScheduleProfiles = 10;

    // ====================================================
    // NHÓM FILE (NESTED CLASS)
    // Chỉ chứa tên file, phần mở rộng
    // ====================================================
    public static class Files
    {
        public const string UserConfig = "UserConfig.bin";
    }

    // ====================================================
    // NHÓM URL (NESTED CLASS)
    // Chứa các đường link liên kết
    // ====================================================
    public static class Urls
    {
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
                Nickname: "LIPEPXIT",
                Bio: "Đụng là cọc",
                AvatarUrl: null,
                SocialLinks: new Dictionary<SocialPlatform, string>
                {
                    // thêm
                },
                Roles: new List<ContributorRole>
                {
                    ContributorRole.Designer
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