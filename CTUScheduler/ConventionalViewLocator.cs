using ReactiveUI;
using Splat;
using System;

namespace CTUScheduler
{
    public class ConventionalViewLocator : IViewLocator
    {
        public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract = null) where TViewModel : class
        {
            return ResolveView(typeof(TViewModel), contract) as IViewFor<TViewModel>;
        }

        public IViewFor? ResolveView(object? viewModel, string? contract = null) 
        {
            if (viewModel is null) return null;
            
            var viewModelType = viewModel is Type t ? t : viewModel.GetType();
            var viewModelName = viewModelType.FullName;
            var viewTypeName = viewModelName?.Replace("ViewModel", "View");

            if (viewTypeName == null) return null;

            try
            {
                var viewType = Type.GetType(viewTypeName);
                if (viewType == null)
                {
                    this.Log().Error($"Could not find the view {viewTypeName} for view model {viewModelName}.");
                    return null;
                }
                return Activator.CreateInstance(viewType) as IViewFor;
            }
            catch (Exception)
            {
                this.Log().Error($"Could not instantiate view {viewTypeName}.");
                throw;
            }
        }
    }

}
