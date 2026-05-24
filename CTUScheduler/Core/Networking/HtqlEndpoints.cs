using System;

namespace CTUScheduler.Core.Networking;

internal static class HtqlEndpoints
{
    private const string HtqlBase = "https://htql.ctu.edu.vn";
    private const string AccountsBase = "https://accounts.ctu.edu.vn";
    private const string DkmhBase = "https://dkmh.ctu.edu.vn";

    public static readonly Uri SessionKey =
        new($"{HtqlBase}/htql/login.php");

    public static readonly Uri PrefetchKey =
        new($"{AccountsBase}/logincontext");

    public static readonly Uri LoginSubmit =
        new($"{AccountsBase}/samlsso");

    public static readonly Uri SsoAuth =
        new($"{DkmhBase}/htql/sinhvien/dang_nhap_sso.php");
}