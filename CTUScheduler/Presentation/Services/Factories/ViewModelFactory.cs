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
    // VM type -> required args type
    private readonly ConcurrentDictionary<Type, Type?> _cache = new();

    public ViewModelFactory(IServiceProvider sp)
    {
        _sp = sp;
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
        
        var requiredArgsType = _cache.GetOrAdd(vmType, type =>
            type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INeedArgs<>))
                ?.GetGenericArguments()[0]
        );
        
        if (requiredArgsType is not null)
        {
            if (requiredArgsType.IsInstanceOfType(args))
                return (IViewModel)ActivatorUtilities.CreateInstance(_sp, vmType, args);
            
            throw new InvalidOperationException(
                $"ViewModel '{vmType.Name}' yêu cầu tham số kiểu '{requiredArgsType.Name}', nhưng bạn lại truyền vào '{args.GetType().Name}'.");
        }
        
        return (IViewModel)_sp.GetRequiredService(vmType);
    }
    
    public IViewModel Create(Type vmType, params object[] inputs)
    {
        EnsureIsViewModel(vmType);
        
        var requiredArgsType = _cache.GetOrAdd(vmType, type =>
            type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INeedArgs<>))
                ?.GetGenericArguments()[0]
        );

        if (requiredArgsType is not null)
        {
          
            bool hasRequiredArg = inputs.Any(x => x != null && requiredArgsType.IsInstanceOfType(x));

            if (!hasRequiredArg)
            {
                throw new InvalidOperationException(
                    $"ViewModel '{vmType.Name}' yêu cầu tham số nghiệp vụ kiểu '{requiredArgsType.Name}', " +
                    "nhưng bạn chưa truyền nó vào lệnh Navigate.");
            }
        }
        
        return (IViewModel)ActivatorUtilities.CreateInstance(_sp, vmType, inputs);
    }


    private void EnsureIsViewModel(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!typeof(IViewModel).IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.Name}' không phải là {nameof(IViewModel)}");
    }
}