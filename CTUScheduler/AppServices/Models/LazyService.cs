using System;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.AppServices.Models;

#pragma warning disable IL2091
public class LazyService<T> : Lazy<T> where T : class
{
    public LazyService(IServiceProvider provider) : base(provider.GetRequiredService<T>)
    {
    }
}
#pragma warning restore IL2091