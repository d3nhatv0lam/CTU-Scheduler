
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Presentation.Services.Adapter;

public interface IAdapter<in TViewmodel>: IUpdatableAsync where TViewmodel : class
{
    void Register(TViewmodel viewModel);
    void Unregister(TViewmodel viewModel);
    
    void UnregisterAll();
}