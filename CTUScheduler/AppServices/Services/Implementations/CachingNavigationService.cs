using CTUScheduler.AppServices.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;


namespace CTUScheduler.AppServices.Services.Implementations
{
    public class CachingNavigationService : ICachingNavigationService
    {
        private readonly IScreen _hostScreen;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private IRoutableViewModel _rootViewmodel = null!;

        public CachingNavigationService(IScreen hostScreen)
        {
            _hostScreen = hostScreen ?? throw new ArgumentNullException(nameof(hostScreen));
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task NavigateTo<TViewmodel>() where TViewmodel : IRoutableViewModel
        {
            var viewModel = this.GetOrCreateViewModel<TViewmodel>();
            _rootViewmodel ??= viewModel;
            await _hostScreen.Router.Navigate.Execute(viewModel).ToTask();
        }

        public async Task NavigateAndResetTo<TViewmodel>() where TViewmodel : IRoutableViewModel
        {
            this.AddCache<TViewmodel>();
            var viewModel = this.GetOrCreateViewModel<TViewmodel>();
            _rootViewmodel = viewModel;
            await _hostScreen.Router.NavigateAndReset.Execute(viewModel).ToTask();
        }

        public void ClearCache<TViewmodel>() where TViewmodel : IRoutableViewModel
        {
            _cache.Remove(typeof(TViewmodel));
        }

        public void ClearAllCache()
        {
            (_cache as MemoryCache)?.Compact(1.0);
        }

        private TViewmodel GetOrCreateViewModel<TViewmodel>() where TViewmodel : IRoutableViewModel
        {
            if (_cache.TryGetValue(typeof(TViewmodel), out TViewmodel? viewModel))
                return viewModel!;

            viewModel = (TViewmodel)Activator.CreateInstance(typeof(TViewmodel), _hostScreen)!;
            return viewModel;
        }

        private void AddCache<TViewModel>() where TViewModel:IRoutableViewModel
        {
            if (_rootViewmodel == null) return;
            
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = _cacheDuration,
            };

            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
            _cache.Set(typeof(TViewModel), _rootViewmodel, options);

            _rootViewmodel = null!;
        }
    }
}
