using System;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Infrastructure.Sites.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Abstractions;

public interface IMainPage: ISitePage
{
    IObservable<string> UserInfoChanges { get;  }
    Task<string> GetUserInfoAsync(CancellationToken cancellationToken = default);
    Task NavigateToDkmhAsync();
}