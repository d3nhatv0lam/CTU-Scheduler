using System.Threading.Tasks;
using ReactiveUI;

namespace CTUScheduler.AppServices.Services.Navigation
{
    public interface ICachingNavigationService
    {
        Task NavigateTo<TViewmodel>() where TViewmodel : IRoutableViewModel;
        Task NavigateAndResetTo<TViewmodel>() where TViewmodel : IRoutableViewModel;
        void ClearCache<TViewmodel>() where TViewmodel : IRoutableViewModel;
        void ClearAllCache();
    }
}
