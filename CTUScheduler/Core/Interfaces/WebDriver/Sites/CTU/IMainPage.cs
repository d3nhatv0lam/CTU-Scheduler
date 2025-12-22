using System;
using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;

public interface IMainPage: ISitePage
{
    IObservable<string> UserInfoChanges { get;  }
    Task<string> GetUserInfoAsync(CancellationToken cancellationToken = default);
    Task NavigateToDkmhAsync();
}