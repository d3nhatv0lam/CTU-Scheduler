using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler;

public class MsDiViewLocator: IViewLocator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MsDiViewLocator> _logger;
    
    public MsDiViewLocator(IServiceProvider serviceProvider, ILogger<MsDiViewLocator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract = null) where TViewModel : class
    {
        return _serviceProvider.GetService<IViewFor<TViewModel>>();    
    }

    public IViewFor? ResolveView(object? viewModel, string? contract = null)
    {
        if (viewModel == null) return null;

        var viewType = typeof(IViewFor<>).MakeGenericType(viewModel.GetType());
        return _serviceProvider.GetService(viewType) as IViewFor;
    }
}