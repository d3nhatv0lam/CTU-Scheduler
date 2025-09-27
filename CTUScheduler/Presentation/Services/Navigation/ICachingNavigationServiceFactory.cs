using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public interface ICachingNavigationServiceFactory
    {
        public ICachingNavigationService Create(IScreen hostScreen);
    }
}
