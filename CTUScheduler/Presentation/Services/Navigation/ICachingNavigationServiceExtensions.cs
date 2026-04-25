using System.Threading.Tasks;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation;

public static class ICachingNavigationServiceExtensions
{
    public static Task NavigateTo<T>(this ICachingNavigationService service, object? args = null)
        where T : class, IRoutableViewModel
        => service.NavigateTo(typeof(T), args);
    
    public static Task NavigateAndResetTo<T>(this ICachingNavigationService service, object? args = null)
        where T : class, IRoutableViewModel
        => service.NavigateAndResetTo(typeof(T), args);
}