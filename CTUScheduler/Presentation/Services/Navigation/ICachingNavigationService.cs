using System;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Shared.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public interface ICachingNavigationService : IDisposable
    {
        Task NavigateTo(Type vmType, object? args = null);
        Task NavigateAndResetTo(Type vmType , object? args = null);
        void ClearCache(Type vmType);
        void ClearAllCache();
    }
}
