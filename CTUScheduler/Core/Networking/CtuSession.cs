using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Networking;

public record CtuSession(
    // API Đăng ký môn học mới (JWT Subdomain)
    string DkmhApiJwtToken,
    DateTimeOffset DkmhApiJwtExpiresAt,
    // Web cũ/Legacy (Cookies)
    Dictionary<string, string> LegacyWebCookies,
    DateTimeOffset LegacyWebCookiesExpiresAt,
    StudentProfile Profile
)
{
    public string StudentId => Profile.Mssv;

    public string StudentName => Profile.Name;

    // Hệ thống tự động logout khi một trong hai cái hết hạn
    public bool IsExpired =>
        DateTimeOffset.Now >= LegacyWebCookiesExpiresAt ||
        DateTimeOffset.Now >= DkmhApiJwtExpiresAt;
}