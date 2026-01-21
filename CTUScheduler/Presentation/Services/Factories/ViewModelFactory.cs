using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.Factories;

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ViewModelFactory> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ConcurrentDictionary<Type, InjectionMetadata[]> _cache = new();
    private readonly TimeSpan _asyncInitTimeout = TimeSpan.FromSeconds(30);


    public ViewModelFactory(IServiceProvider sp, IHostApplicationLifetime appLifetime,  ILogger<ViewModelFactory> logger)
    {
        _sp = sp;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public IViewModel Create(Type vmType)
    {
        ArgumentNullException.ThrowIfNull(vmType);

        if (!typeof(IViewModel).IsAssignableFrom(vmType))
            throw new ArgumentException($"Type '{vmType.Name}' không phải là IViewModel");

        return (IViewModel)_sp.GetRequiredService(vmType);
    }
    public IViewModel Create(Type vmType, object args)
    {
        ArgumentNullException.ThrowIfNull(vmType);
        ArgumentNullException.ThrowIfNull(args);
        
        var strategies = _cache.GetOrAdd(vmType, ScanTypeForStrategies);

        foreach (var strategy in strategies)
        {
            if (!strategy.ArgType.IsInstanceOfType(args)) continue;
            
            switch (strategy.Type)
            {
                case InjectionType.Constructor:
                    return (IViewModel)ActivatorUtilities.CreateInstance(_sp, vmType, args);

                case InjectionType.Method:
                {
                    var vm = (IViewModel)_sp.GetRequiredService(vmType);
                    strategy.InitMethod!.Invoke(vm, [args]);
                    return vm;
                }

                case InjectionType.AsyncMethod: 
                {
                    var vm = (IViewModel)_sp.GetRequiredService(vmType);
                    
                    var shutdownToken = _appLifetime.ApplicationStopping; 
                    var task = (Task)strategy.InitMethod!.Invoke(vm, [args, shutdownToken])!;
                    
                    HandleAsyncInitSafely(task, vmType);
                    
                    return vm;
                }
            }
        }
        
        throw new InvalidOperationException(
            $"ViewModel '{vmType.Name}' không có Interface (INeedArgs/IInitializable) phù hợp với tham số kiểu '{args.GetType().Name}'.");
    }
    
    private async void HandleAsyncInitSafely(Task task, Type vmType)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Initialization timed out or cancelled for {Type}", vmType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Async initialization crashed for {Type}", vmType.Name);
        }
    }
    
    private static InjectionMetadata[] ScanTypeForStrategies(Type type)
    {
        var results = new List<InjectionMetadata>();
        var interfaces = type.GetInterfaces();

        foreach (var i in interfaces)
        {
            if (!i.IsGenericType) continue;
            var def = i.GetGenericTypeDefinition();
            var argType = i.GetGenericArguments()[0];

            if (def == typeof(INeedArgs<>))
            {
                results.Add(new InjectionMetadata(InjectionType.Constructor, argType, null));
            }
            else if (def == typeof(IInitializable<>))
            {
                var method = i.GetMethod("Initialize");
                results.Add(new InjectionMetadata(InjectionType.Method, argType, method));
            }
            else if (def == typeof(IAsyncInitializable<>))
            {
                var method = i.GetMethod("InitializeAsync");
                results.Add(new InjectionMetadata(InjectionType.AsyncMethod, argType, method));
            }
        }
        // Sắp xếp ưu tiên: Constructor -> Initialize (Đồng bộ) -> InitializeAsync (Bất đồng bộ)
        return results.OrderBy(x => x.Type).ToArray();
    }
    
    private enum InjectionType { Constructor, Method , AsyncMethod}
    private record InjectionMetadata(InjectionType Type, Type ArgType, MethodInfo? InitMethod);
}