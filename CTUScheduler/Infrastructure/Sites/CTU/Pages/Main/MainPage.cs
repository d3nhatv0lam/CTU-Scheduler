using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading;

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



    public async Task<string> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAsync()) return string.Empty;

        return await Tab.NativePage.Locator(UserInfoSelector).InnerTextAsync();
    }

    public async Task NavigateToDkmhAsync()
    {
        await SecureClickAsync(DkmhButtonSelector);
    }
}