using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.DriverCore.Refactor.Page;

public class MainPage: AppPage, IRequireSession
{
    public override string PageUrl => "https://dkmh.ctu.edu.vn/htql/sinhvien/hindex.php";
    
    protected override string PageReadySelector => UserInfoSelector; 

    private const string UserInfoSelector = "#user-login";
    private const string DkmhButtonSelector = "img[src*=\"hetinchi.gif\"][onclick*=\"gotoDKindex\"]";

    public MainPage(IWebTab tab, ILoggerFactory logger) : base(tab, logger) { }

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
        
        return await Tab.GetTextAsync(UserInfoSelector);
    }

    public async Task NavigateToDkmhAsync()
    {
        await Tab.ClickAsync(DkmhButtonSelector);
    }
}