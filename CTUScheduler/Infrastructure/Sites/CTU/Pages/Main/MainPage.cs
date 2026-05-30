using System;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using Microsoft.Playwright;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;

public class MainPage : AppPage, IRequireSession, IMainPage
{
    public override string PageUrl => "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    protected override string PageReadySelector => UserInfoSelector;

    private const string UserInfoSelector = "#user-login";
    private const string DkmhButtonSelector = "img[src*=\"hetinchi.gif\"][onclick*=\"gotoDKindex\"]";

    public MainPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory logger) : base(tab,
        connectivityService, logger)
    {
    }



    public async Task<StudentProfile?> GetStudentProfileAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAsync()) return null;

        var locator = Tab.NativePage.Locator(UserInfoSelector);
        try
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            var userText = await locator.InnerTextAsync();

            if (string.IsNullOrWhiteSpace(userText)) return null;

            // Format: "Dương Minh Đức (B2303807)"
            var parts = userText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var mssv = parts[^1].Trim('(', ')', ' ', '\t', ',');
                var name = string.Join(" ", parts[..^1]).Trim();
                // return new StudentProfile(mssv, name);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Fail to get student profile from legacy Home page");
        }

        return null;
    }

    public async Task NavigateToDkmhAsync()
    {
        await SecureClickAsync(DkmhButtonSelector);
    }
}