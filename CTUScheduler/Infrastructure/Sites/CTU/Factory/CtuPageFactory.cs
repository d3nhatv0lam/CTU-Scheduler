using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.Base;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Infrastructure.Sites.CTU.Factory;

public class CtuPageFactory: ICtuPageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _pageMappings = new();
    
    public CtuPageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        if (_pageMappings.Count == 0)
        {
            var assembly = typeof(AppPage).Assembly;
            var implementations = assembly.GetTypes()
                .Where(t => typeof(ISitePage).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

            foreach (var impl in implementations)
            {
                var serviceInterface = impl.GetInterfaces()
                    .FirstOrDefault(i => i.Name == $"I{impl.Name}");
                
                if (serviceInterface != null)
                    _pageMappings[serviceInterface] = impl;
                
                _pageMappings[impl] = impl;
            }
        }
    }

    public T GetPage<T>(IWebTab tab) where T : class, ISitePage
    {
        var interfaceType = typeof(T);

        if (!_pageMappings.TryGetValue(interfaceType, out var implementationType))
        {
            throw new InvalidOperationException($"Chưa đăng ký Page cho Interface: {interfaceType.Name}");
        }
        
        return (T)ActivatorUtilities.CreateInstance(_serviceProvider, implementationType, tab);
    }
    
}