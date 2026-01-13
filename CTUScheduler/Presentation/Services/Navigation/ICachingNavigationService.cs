using System;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public interface ICachingNavigationService : IDisposable
    {
        Task NavigateTo(Type vmType);
        Task NavigateAndResetTo(Type vmType);
        void ClearCache(Type vmType);
        void ClearAllCache();
    }
}
