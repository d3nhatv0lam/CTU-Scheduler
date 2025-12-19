using System;
using CTUScheduler.Core.Interfaces.WebDriver;
using CTUScheduler.Core.Interfaces.WebDriver.Sites;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Infrastructure.Sites.Base;

public abstract class BaseSitePageFactory<TRootPage>: ISitePageFactory<TRootPage> where TRootPage: ISitePage
{
    private readonly IServiceProvider _serviceProvider;
    public string SiteName => GetType().Name;
    
    protected BaseSitePageFactory(IServiceProvider provider)
    {
        _serviceProvider = provider;
    }
    
    public virtual T GetPage<T>() where T : TRootPage
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}