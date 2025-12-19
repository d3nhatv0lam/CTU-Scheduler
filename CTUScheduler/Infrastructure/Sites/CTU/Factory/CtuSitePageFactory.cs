using System;
using CTUScheduler.Core.Interfaces.WebDriver.Sites;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Pages.Base;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public class CtuSitePageFactory: BaseSitePageFactory<ISitePage>, ICtuSitePageFactory
{
    public CtuSitePageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}