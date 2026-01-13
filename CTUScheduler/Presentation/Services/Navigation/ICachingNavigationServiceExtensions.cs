using System.Threading.Tasks;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation;

public static class ICachingNavigationServiceExtensions
{
    public static Task NavigateTo<T>(this ICachingNavigationService service) where T : class, IRoutableViewModel 
        => service.NavigateTo(typeof(T));

    public static Task NavigateAndResetTo<T>(this ICachingNavigationService service) where T : class, IRoutableViewModel 
        => service.NavigateAndResetTo(typeof(T));
}