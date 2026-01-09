using System;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CTUScheduler.Presentation.Services.Factories;

public class ViewModelFactory: IViewModelFactory
{
    private readonly IServiceProvider _sp;

    public ViewModelFactory(IServiceProvider sp)
    {
        _sp = sp;
    }
    
    public TVM Create<TVM>() where TVM : class, IViewModel
    {
        return _sp.GetRequiredService<TVM>();
    }
   
    public TVM Create<TVM, TContext>(TContext args) 
        where TVM : class, IViewModel
    {
        if (args is null) throw new ArgumentNullException(nameof(args));
        
        var typeVM = typeof(TVM);
        var typeArgs = typeof(TContext);
        
        // INeedArgs
        if (typeof(INeedArgs<TContext>).IsAssignableFrom(typeVM))
        {
            return ActivatorUtilities.CreateInstance<TVM>(_sp, args);
        }
        
        // IInitializable
        if (typeof(IInitializable<TContext>).IsAssignableFrom(typeof(TVM)))
        {
            var vm = _sp.GetRequiredService<TVM>();
            (vm as IInitializable<TContext>)!.Init(args);
            return vm;
        }

        throw new InvalidOperationException(
            $"ViewModel '{typeVM.Name}' không hỗ trợ tham số '{typeArgs.Name}'.\n" +
            $"Vui lòng implement 'INeedArgs<{typeArgs.Name}>' (nếu muốn tạo mới) " +
            $"hoặc 'IInitializable<{typeArgs.Name}>' (nếu muốn dùng Singleton/DI).");
    }
}