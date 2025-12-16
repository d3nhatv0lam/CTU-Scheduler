using System;
using CTUScheduler.Core.Interfaces.WebDriver;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Infrastructure.Sites.Base;

public abstract class BaseSitePageFactory<TRootPage>: ISitePageFactory<TRootPage> where TRootPage: class
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