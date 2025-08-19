using ReactiveUI;

namespace CTUScheduler.AppServices.Services.Navigation
{
    public interface ICachingNavigationServiceFactory
    {
        public ICachingNavigationService Create(IScreen hostScreen);
    }
}
