using System;

namespace CTUScheduler.Core.Networking;

internal static class DkmhEndpoints
{
    private const string WebBase =
        "https://dkmh.ctu.edu.vn";

    private const string ApiBase =
        "https://dkmhback.ctu.edu.vn/api";

    private const string DangKyHocPhanBase =
        $"{ApiBase}/v1/dangkyhocphan";

    public static readonly Uri BaseDomain = new(WebBase);

    public static readonly Uri Login =
        new($"{WebBase}/htql/dkmh/student/dang_nhap.php");

    public static readonly Uri GetToken =
        new($"{ApiBase}/auth/saml2/acs");

    public static readonly Uri HocPhan =
        new($"{DangKyHocPhanBase}/sinhvien/danhmuchocphan");

    public static readonly Uri QuyDinh =
        new($"{DangKyHocPhanBase}/sinhvien/quydinhdangky");

    public static readonly Uri DaDangKy =
        new($"{DangKyHocPhanBase}/hocphandadangky");

    public static readonly Uri HocPhi =
        new($"{DangKyHocPhanBase}/sinhvien/thongtinhocphi");
}