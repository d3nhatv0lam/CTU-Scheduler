using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Services.Navigation
{
    public class CachingNavigationService : ICachingNavigationService
    {
        private readonly IScreen _hostScreen;
        private readonly IViewModelFactory _vmFactory;
        private readonly ConcurrentDictionary<Type, Lazy<IRoutableViewModel>> _cache = new();
        private readonly ILogger<CachingNavigationService> _logger;

        public CachingNavigationService(IScreen hostScreen, IViewModelFactory vmFactory,
            ILogger<CachingNavigationService> logger)
        {
            _hostScreen = hostScreen ?? throw new ArgumentNullException(nameof(hostScreen));
            _vmFactory = vmFactory ?? throw new ArgumentNullException(nameof(vmFactory));
            _logger = logger;
            _logger.LogDebug("CachingNavigationService initialized for Screen: {ScreenType}",
                hostScreen.GetType().Name);
        }

        // public Task NavigateTo<TViewModel>() where TViewModel : class, IRoutableViewModel, IViewModel
        //     => ExecuteNavigation<TViewModel>(reset: false);
        //
        // public Task NavigateAndResetTo<TViewModel>() where TViewModel : class, IRoutableViewModel, IViewModel
        //     => ExecuteNavigation<TViewModel>(reset: true);
        // public void ClearCache<TViewModel>() where TViewModel : class, IRoutableViewModel, IViewModel
        //     => ClearCacheInternal(type => type == typeof(TViewModel));
        //
        // public void ClearAllCache() => ClearCacheInternal();
        //
        // private TViewModel GetOrCreateViewModel<TViewModel>() where TViewModel : class, IRoutableViewModel, IViewModel
        // {
        //     var type = typeof(TViewModel);
        //     var lazyVm = _cache.GetOrAdd(type, _ =>
        //         new Lazy<IRoutableViewModel>(() =>
        //             _vmFactory.Create<TViewModel, IScreen>(_hostScreen)
        //         )
        //     );
        //     try
        //     {
        //         return (TViewModel)lazyVm.Value;
        //     }
        //     catch
        //     {
        //         _cache.TryRemove(type, out _);
        //         throw;
        //     }
        // }
        //
        // private async Task ExecuteNavigation<TViewModel>(bool reset)
        //     where TViewModel : class, IRoutableViewModel, IViewModel
        // {
        //     var targetType = typeof(TViewModel);
        //     _logger.LogInformation("Navigating to {ViewModel} (Reset: {IsReset})", targetType, reset);
        //     var viewModel = GetOrCreateViewModel<TViewModel>();
        //     if (reset)
        //     {
        //         await _hostScreen.Router.NavigateAndReset.Execute(viewModel).ToTask();
        //         ClearCacheInternal(type => type != targetType);
        //     }
        //     else
        //         await _hostScreen.Router.Navigate.Execute(viewModel).ToTask();
        // }
        //
        // private void ClearCacheInternal(Func<Type, bool>? predicate = null)
        // {
        //     var keysToRemove = predicate == null
        //         ? _cache.Keys.ToList()
        //         : _cache.Keys.Where(predicate).ToList();
        //
        //     foreach (var key in keysToRemove)
        //     {
        //         if (_cache.TryRemove(key, out var lazyVm))
        //         {
        //             if (lazyVm is { IsValueCreated: true, Value: IDisposable disposable })
        //             {
        //                 _logger.LogDebug("Disposing and removing {ViewModel} from cache", key.Name);
        //                 try 
        //                 {
        //                     disposable.Dispose();
        //                 }
        //                 catch (Exception ex)
        //                 {
        //                     _logger.LogError(ex, "Failed to dispose ViewModel of type {ViewModelType}", key.Name);
        //                 }
        //             }
        //         }
        //     }
        // }
        //
        // public void Dispose()
        // {
        //     _logger.LogInformation("Disposing CachingNavigationService and clearing all cache");
        //     ClearAllCache();
        // }
        public Task NavigateTo(Type vmType) => ExecuteNavigation(vmType, false);
        public Task NavigateAndResetTo(Type vmType) => ExecuteNavigation(vmType, true);
        public void ClearCache(Type vmType) => ClearCacheInternal(t => t == vmType);
        public void ClearAllCache() => ClearCacheInternal();
        private async Task ExecuteNavigation(Type vmType, bool reset)
        {
            if (!typeof(IRoutableViewModel).IsAssignableFrom(vmType))
                throw new ArgumentException($"{vmType.Name} must implement IRoutableViewModel");

            if (reset)
            {
                _logger.LogDebug("Resetting navigation for {Type}", vmType.Name);
                ClearCacheInternal(t => t != vmType);
            }

            var viewModel = GetOrCreateViewModel(vmType);

            if (reset)
                await _hostScreen.Router.NavigateAndReset.Execute(viewModel).ToTask();
            else
                await _hostScreen.Router.Navigate.Execute(viewModel).ToTask();
        }

        private IRoutableViewModel GetOrCreateViewModel(Type vmType)
        {
            return _cache.GetOrAdd(vmType, t => new Lazy<IRoutableViewModel>(() =>
            {
                _logger.LogTrace("Creating new instance for {Type}", t.Name);
                return (IRoutableViewModel)_vmFactory.Create(t, _hostScreen);
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
                        try { d.Dispose(); } 
                        catch (Exception ex) { _logger.LogError(ex, "Dispose error {Type}", key.Name); }
                    }
                }
            }
        }
        public void Dispose() => ClearAllCache();
    }
}