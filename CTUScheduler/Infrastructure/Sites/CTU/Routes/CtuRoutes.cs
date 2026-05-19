using System.Linq;
using System.Text.RegularExpressions;

namespace CTUScheduler.Infrastructure.Sites.CTU.Routes;

public static class CtuRoutes
{
    public static readonly string[] AuthRedirectSignatures =
    [
        "login.do", 
        "authenticationendpoint"
    ];
    
    public static readonly Regex AuthRedirectRegex = new(
        string.Join("|", AuthRedirectSignatures.Select(Regex.Escape)),
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    
    public const string HtqlRoot = "https://htql.ctu.edu.vn/";
    public const string HtqlMain = "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    
    
    public const string DkmhRoot = "https://dkmhfe.ctu.edu.vn/";
    // Các route con của ĐKMH
    public const string DkmhCatalog = "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/danhmuchocphan";
    public const string DkmhRules = "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/quydinhdangky";
    public const string DkmhRegistration = "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/dangkyhocphan";
    public const string DkmhSchedule= "https://dkmhfe.ctu.edu.vn/dangkyhocphan/sinhvien/thoikhoabieu";
}