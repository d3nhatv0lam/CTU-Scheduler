using CTUScheduler.Presentation.Shared.Interfaces;

namespace CTUScheduler.Presentation.Services.Factories;

public interface IViewModelFactory
{
    TVM Create<TVM, TContext>(TContext args) where TVM : class, IViewModel;
    TVM Create<TVM>() where TVM : class, IViewModel;
}