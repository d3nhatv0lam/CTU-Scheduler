using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public class CachingNavigationService : ICachingNavigationService, IDisposable
    {
        private readonly IScreen _hostScreen;
        private readonly IViewModelFactory _vmFactory;
        private readonly ConcurrentDictionary<Type, Lazy<IRoutableViewModel>> _cache = new();
        private readonly ILogger<CachingNavigationService> _logger;

        public CachingNavigationService(IScreen hostScreen, IViewModelFactory vmFactory,
            ILogger<CachingNavigationService> logger)
        {
            ArgumentNullException.ThrowIfNull(hostScreen);
            ArgumentNullException.ThrowIfNull(vmFactory);
            ArgumentNullException.ThrowIfNull(logger);
            _hostScreen = hostScreen ;
            _vmFactory = vmFactory;
            _logger = logger;
            _logger.LogDebug("CachingNavigationService initialized for Screen: {ScreenType}",
                hostScreen.GetType().Name);
        }

        public Task NavigateTo(Type vmType, object? args = null) => ExecuteNavigation(vmType, args, false);
        public Task NavigateAndResetTo(Type vmType, object? args = null) => ExecuteNavigation(vmType, args, true);

        private async Task ExecuteNavigation(Type vmType, object? args, bool reset)
        {
            if (!typeof(IRoutableViewModel).IsAssignableFrom(vmType))
                throw new ArgumentException($"{vmType.Name} must implement IRoutableViewModel");

            if (reset)
            {
                _logger.LogDebug("Resetting navigation for {Type}", vmType.Name);
                ClearCacheInternal(t => t != vmType);
            }

            var viewModel = GetOrCreateViewModel(vmType, args);

            if (args != null)
            {
                if (viewModel is IInitializable init) init.Initialize(args);
                else if (viewModel is IAsyncInitializable asyncInit) await asyncInit.InitializeAsync(args);
            }

            if (reset)
                await _hostScreen.Router.NavigateAndReset.Execute(viewModel).ToTask();
            else
                await _hostScreen.Router.Navigate.Execute(viewModel).ToTask();
        }

        private IRoutableViewModel GetOrCreateViewModel(Type vmType, object? args = null)
        {
            bool skipCache = typeof(INonCacheable).IsAssignableFrom(vmType);

            object[] inputs = args != null
                ? [_hostScreen, args]
                : [_hostScreen];

            if (skipCache)
            {
                _logger.LogTrace("Creating new instance for {Type}", vmType.Name);
                if (_cache.TryRemove(vmType, out var oldLazy) && oldLazy.IsValueCreated)
                {
                    (oldLazy.Value as IDisposable)?.Dispose();
                }

                return (IRoutableViewModel)_vmFactory.Create(vmType, inputs);
            }

            return _cache.GetOrAdd(vmType, type => new Lazy<IRoutableViewModel>(() =>
            {
                _logger.LogTrace("Creating new instance for {Type}", type.Name);
                return (IRoutableViewModel)_vmFactory.Create(type, inputs);
            })).Value;
        }

        private void ClearCacheInternal(Func<Type, bool>? predicate = null)
        {
            var keys = predicate == null ? _cache.Keys.ToList() : _cache.Keys.Where(predicate).ToList();
            foreach (var key in keys)
            {
                if (_cache.TryRemove(key, out var lazy) && lazy.IsValueCreated)
                {
                    if (lazy.Value is IDisposable d)
                    {
                        try
                        {
                            d.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Dispose error {Type}", key.Name);
                        }
                    }
                }
            }
        }

        public void ClearCache(Type vmType) => ClearCacheInternal(t => t == vmType);
        public void ClearAllCache() => ClearCacheInternal();
        public void Dispose() => ClearAllCache();
    }
}