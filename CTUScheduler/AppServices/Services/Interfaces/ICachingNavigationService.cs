using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface ICachingNavigationService
    {
        Task NavigateTo<TViewmodel>() where TViewmodel : IRoutableViewModel;
        Task NavigateAndResetTo<TViewmodel>() where TViewmodel : IRoutableViewModel;
        void ClearCache<TViewmodel>() where TViewmodel : IRoutableViewModel;
        void ClearAllCache();
    }
}
