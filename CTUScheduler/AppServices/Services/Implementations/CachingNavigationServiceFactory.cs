using CTUScheduler.AppServices.Services.Interfaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
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
