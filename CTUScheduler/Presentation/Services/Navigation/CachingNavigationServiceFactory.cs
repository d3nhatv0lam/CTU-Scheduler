using System;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public class CachingNavigationServiceFactory : ICachingNavigationServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public CachingNavigationServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICachingNavigationService Create(IScreen hostScreen)
        {
            return new CachingNavigationService(hostScreen);
        }
    }
}
