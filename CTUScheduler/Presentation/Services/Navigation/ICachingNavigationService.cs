using System;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public interface ICachingNavigationService
    {
        Task NavigateTo(Type vmType, object? args = null);
        Task NavigateAndResetTo(Type vmType , object? args = null);
        void ClearCache(Type vmType);
        void ClearAllCache();
    }
}
