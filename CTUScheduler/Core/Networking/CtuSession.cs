using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Networking;

public record CtuSession(
    // API Đăng ký môn học mới (JWT Subdomain)
    string DkmhApiJwtToken,
    DateTimeOffset DkmhApiJwtExpiresAt,
    // Web cũ/Legacy (Cookies)
    IDictionary<string, string> LegacyWebCookies,
    DateTimeOffset LegacyWebCookiesExpiresAt,
    StudentProfile Profile
)
{
    public string StudentId => Profile.Mssv;

    public string StudentName => Profile.Name;

    // Hệ thống tự động logout khi một trong hai cái hết hạn
    public bool IsExpired =>
        DateTimeOffset.UtcNow >= LegacyWebCookiesExpiresAt ||
        DateTimeOffset.UtcNow >= DkmhApiJwtExpiresAt;
    
    public override string ToString()
    {
        var cookiesStr = string.Join(", ", LegacyWebCookies.Select(kv => $"{kv.Key}={kv.Value}"));
        
        return $"CtuSession {{ \n" +
               $"  DkmhApiJwtToken = {DkmhApiJwtToken},\n" +
               $"  DkmhApiJwtExpiresAt = {DkmhApiJwtExpiresAt.ToLocalTime():dd/MM/yyyy HH:mm:ss},\n" +
               $"  LegacyWebCookies = [ {cookiesStr} ],\n" +
               $"  LegacyWebCookiesExpiresAt = {LegacyWebCookiesExpiresAt.ToLocalTime():dd/MM/yyyy HH:mm:ss},\n" +
               $"  Profile = {Profile}\n" +
               $"}}";
    }
}