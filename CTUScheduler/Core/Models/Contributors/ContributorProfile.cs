using System.Collections.Generic;

namespace CTUScheduler.Core.Models.Contributors;

public record ContributorProfile(
    string Id,
    string Name,
    ContributorTier Tier,
    string? Nickname = null,
    string? Bio = null,
    string? AvatarUrl = null,
    IReadOnlyDictionary<SocialPlatform, string>? SocialLinks = null, 
    IReadOnlyList<ContributorRole>? Roles = null,
    int DisplayOrder = 0
)
{
    public IReadOnlyDictionary<SocialPlatform, string> SocialLinks { get; init; } 
        = SocialLinks ?? new Dictionary<SocialPlatform, string>();

    public IReadOnlyList<ContributorRole> Roles { get; init; } 
        = Roles ?? [];
    
    public string DisplayName => !string.IsNullOrWhiteSpace(Nickname) ? Nickname : Name;
}