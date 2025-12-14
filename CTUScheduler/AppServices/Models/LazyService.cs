using System;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.AppServices.Models;

public class LazyService<T>: Lazy<T> where T : class
{
    public LazyService(IServiceProvider provider): base(provider.GetRequiredService<T>) {}
}