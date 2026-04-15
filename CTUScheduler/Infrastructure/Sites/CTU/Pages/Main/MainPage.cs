using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Infrastructure.DriverCore.Refactor;
using CTUScheduler.Infrastructure.Sites.Base;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Sites.CTU.Pages.Main;

public class MainPage : AppPage, IRequireSession
{
    public override string PageUrl => "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    protected override string PageReadySelector => UserInfoSelector;

    private const string UserInfoSelector = "#user-login";
    private const string DkmhButtonSelector = "img[src*=\"hetinchi.gif\"][onclick*=\"gotoDKindex\"]";

    public MainPage(IWebTab tab, IConnectivityService connectivityService, ILoggerFactory logger) : base(tab,
        connectivityService, logger)
    {
    }

    public IObservable<string> UserInfoChanges => Observable
        .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2))
        .SelectMany(async _ => await IsActiveAsync() ? await GetUserInfoAsync() : string.Empty)
        .DistinctUntilChanged()
        .Catch((Exception ex) =>
        {
            Logger.LogWarning(ex, "Fail when pulling GetUserInfoAsync");
            return Observable.Return(string.Empty);
        });

    public async Task<string> GetUserInfoAsync()
    {
        if (!await IsActiveAsync()) return string.Empty;

        return await Tab.NativePage.Locator(UserInfoSelector).InnerTextAsync();
    }

    public async Task NavigateToDkmhAsync()
    {
        await Tab.NativePage.ClickAsync(DkmhButtonSelector);
    }
}