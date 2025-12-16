using System;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public class CtuSitePageFactory: BaseSitePageFactory<CtuBasePage>, ICtuSitePageFactory
{
    private readonly Lazy<ILoginPage> _lazyLoginPage;
    private readonly Lazy<IMainPage> _lazyMainPage;
    private readonly Lazy<IRegistrationRulesPage> _lazyRegistrationRulesPage;
    
    public ILoginPage LoginPage => _lazyLoginPage.Value;
    public IMainPage MainPage => _lazyMainPage.Value;
    public IRegistrationRulesPage RegistrationRulesPage => _lazyRegistrationRulesPage.Value;
    
    public CtuSitePageFactory(IServiceProvider serviceProvider,
        Lazy<ILoginPage> lazyLoginPage,
        Lazy<IMainPage> lazyMainPage,
        Lazy<IRegistrationRulesPage> lazyRegistrationRulesPage) : base(serviceProvider)
    {

        _lazyLoginPage = lazyLoginPage;
        _lazyMainPage = lazyMainPage;
        _lazyRegistrationRulesPage = lazyRegistrationRulesPage;
    }
    
}