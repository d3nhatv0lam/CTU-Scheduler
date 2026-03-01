using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IApplicationLifetime = CTUScheduler.Presentation.Services.ApplicationLifetime.IApplicationLifetime;

namespace CTUScheduler.Presentation.Services.Factories;

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ViewModelFactory> _logger;
    private readonly IApplicationLifetime _appLifetime;
    private readonly ConcurrentDictionary<Type, InjectionMetadata[]> _cache = new();

    public ViewModelFactory(IServiceProvider sp, IApplicationLifetime appLifetime, ILogger<ViewModelFactory> logger)
    {
        _sp = sp;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public IViewModel Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.Interfaces)]
        Type vmType)
    {
        EnsureIsViewModel(vmType);
        return (IViewModel)_sp.GetRequiredService(vmType);
    }

    public IViewModel Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.Interfaces)]
        Type vmType, object args)
    {
        EnsureIsViewModel(vmType);
        ArgumentNullException.ThrowIfNull(args);

        var strategies = _cache.GetOrAdd(vmType, ScanTypeForStrategies);
        var argType = args.GetType();

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
                    (vm as IInitializable)?.Initialize(args);
                    return vm;
                }

                case InjectionType.AsyncMethod:
                {
                    var vm = (IViewModel)_sp.GetRequiredService(vmType);
                    var task = (vm as IAsyncInitializable)?.InitializeAsync(args, _appLifetime.ApplicationStopping);

                    HandleAsyncInitSafely(task, vmType);
                    return vm;
                }
            }
        }

        throw new InvalidOperationException(
            $"ViewModel '{vmType.Name}' không hỗ trợ tham số kiểu '{argType.Name}'");
    }

    private async void HandleAsyncInitSafely(Task? task, Type vmType)
    {
        if (task is null) return;
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

        foreach (var i in type.GetInterfaces())
        {
            if (!i.IsGenericType) continue;
            var def = i.GetGenericTypeDefinition();
            var argType = i.GetGenericArguments()[0];

            if (def == typeof(INeedArgs<>))
                results.Add(new InjectionMetadata(InjectionType.Constructor, argType));
            else if (def == typeof(IInitializable<>))
                results.Add(new InjectionMetadata(InjectionType.Method, argType));
            else if (def == typeof(IAsyncInitializable<>))
                results.Add(new InjectionMetadata(InjectionType.AsyncMethod, argType));
        }

        // Sắp xếp ưu tiên: Constructor -> Initialize (Đồng bộ) -> InitializeAsync (Bất đồng bộ)
        return results.OrderBy(x => x.Type).ToArray();
    }

    private void EnsureIsViewModel(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!typeof(IViewModel).IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.Name}' không phải là {nameof(IViewModel)}");
    }

    private enum InjectionType
    {
        Constructor,
        Method,
        AsyncMethod
    }

    private record InjectionMetadata(InjectionType Type, Type ArgType);
}