using CTUScheduler.Core.Interfaces.WebDriver;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Login;

namespace CTUScheduler.Infrastructure.Sites.CTU.Adapters;

public interface ICtuDriverAdapter: ISiteAdapter
{
    ILoginPage LoginPage { get; }
}