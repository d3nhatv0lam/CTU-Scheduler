using CTUScheduler.Core.Interfaces.WebDriver;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public interface ICtuSitePageFactory: ISitePageFactory<CtuBasePage>
{
    ILoginPage LoginPage { get; }
    IMainPage MainPage { get; }
    IRegistrationRulesPage RegistrationRulesPage { get; }
}