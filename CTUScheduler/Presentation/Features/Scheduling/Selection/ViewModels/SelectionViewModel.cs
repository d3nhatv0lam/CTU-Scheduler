using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Shared.Models.Regions;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels
{
    public class SelectionViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly INavigationRegionManager _navigationRegionManager;
        private readonly SchedulingStrategy _manualStrategy;
        private readonly SchedulingStrategy _quickStrategy;

        public string? UrlPathSegment => nameof(SelectionViewModel);

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit,Unit> ManualSelectionCommand { get; protected set; }
        public ReactiveCommand<Unit,Unit> QuickSelectionCommand { get; protected set; }
        
        public SelectionViewModel(IScreen hostScreen, 
            INavigationRegionManager navigationRegionManager,
            ManualSchedulingStrategy manualStrategy,
            QuickSchedulingStrategy quickStrategy)
        {
            HostScreen = hostScreen;
            _navigationRegionManager = navigationRegionManager;
            _manualStrategy = manualStrategy;
            _quickStrategy = quickStrategy;

            ManualSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(_manualStrategy)).DisposeWith(_disposables);
            
            var disabledQuickSelection = Observable.Return(false);
            QuickSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(_quickStrategy),disabledQuickSelection).DisposeWith(_disposables);
        }

        private void NavigateToSelection(SchedulingStrategy strategy)
        {
            _navigationRegionManager.NavigateTo<SchedulingShellViewModel>(RegionIds.Scheduling, strategy);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
