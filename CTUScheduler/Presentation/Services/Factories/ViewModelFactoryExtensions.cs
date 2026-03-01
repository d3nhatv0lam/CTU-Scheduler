using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;

namespace CTUScheduler.Presentation.Services.Factories;

public static class ViewModelFactoryExtensions
{
    public static TVM Create<TVM>(this IViewModelFactory factory) 
        where TVM : class, IViewModel
    {
        return (TVM)factory.Create(typeof(TVM));
    }

    public static TVM Create<TVM, TContext>(this IViewModelFactory factory, TContext args) 
        where TVM : class, IViewModel , INeedArgs<TContext> 
        where TContext : notnull
    {
        return (TVM)factory.Create(typeof(TVM), args);
    }
}