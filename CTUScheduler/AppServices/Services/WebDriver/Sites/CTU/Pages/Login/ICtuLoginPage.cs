using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.WebDriver.Interfaces;
using CTUScheduler.AppServices.Services.WebDriver.Models;

namespace CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Pages.Login;

public interface ICtuLoginPage: ISitePage
{
    public Task<LoginResult> TryLoginAsync(string username, string password, CancellationToken cancellationToken = default);
}