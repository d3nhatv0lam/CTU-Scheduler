using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Networking;

public record DkmhSession(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    StudentProfile Profile
)
{
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}

public record HtqlSession(
    IDictionary<string, string> Cookies,
    DateTimeOffset ExpiresAt
)
{
    public Guid InstanceId { get; init; } = Guid.NewGuid();

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}

public record CtuSession(
    DkmhSession Dkmh,
    HtqlSession? Htql
)
{
    public StudentProfile Profile => Dkmh.Profile;

    public string StudentId => Profile.Mssv;

    // Chỉ kết thúc phiên chính khi phân hệ Dkmh (JWT) hết hạn
    public bool IsExpired => Dkmh.IsExpired;

    public bool IsHtqlExpired => Htql == null || Htql.IsExpired;

    public override string ToString()
    {
        var cookiesStr = Htql != null
            ? string.Join(", ", Htql.Cookies.Select(kv => $"{kv.Key}={kv.Value}"))
            : "null";

        var htqlExpiresStr = Htql != null
            ? Htql.ExpiresAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
            : "null";

        return $"CtuSession {{ \n" +
               $"  Dkmh = DkmhSession {{ AccessToken = {Dkmh.AccessToken}, ExpiresAt = {Dkmh.ExpiresAt.ToLocalTime():dd/MM/yyyy HH:mm:ss} }},\n" +
               $"  Htql = HtqlSession {{ InstanceId = {Htql?.InstanceId.ToString() ?? "null"}, Cookies = [ {cookiesStr} ], ExpiresAt = {htqlExpiresStr} }},\n" +
               $"  Profile = {Profile}\n" +
               $"}}";
    }
}