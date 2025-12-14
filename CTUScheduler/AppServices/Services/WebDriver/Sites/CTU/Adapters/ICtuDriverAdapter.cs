using CTUScheduler.AppServices.Services.WebDriver.Interfaces;
using CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Pages.Login;

namespace CTUScheduler.AppServices.Services.WebDriver.Sites.CTU.Adapters;

public interface ICtuDriverAdapter: ISiteAdapter
{
    ICtuLoginPage CtuLoginPage { get; }
}