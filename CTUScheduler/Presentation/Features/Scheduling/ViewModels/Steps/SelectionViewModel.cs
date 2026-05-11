using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps
{
    public class SelectionViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly INavigationRegionManager _navigationRegionManager;

        public string UrlPathSegment => nameof(SelectionViewModel);
        public IScreen HostScreen { get; }
        public ReactiveCommand<Unit, Unit> ManualSelectionCommand { get; }
        public ReactiveCommand<Unit, Unit> QuickSelectionCommand { get; }

        public SelectionViewModel(IScreen hostScreen,
            INavigationRegionManager navigationRegionManager,
            ManualSchedulingStrategy manualStrategy,
            QuickSchedulingStrategy quickStrategy)
        {
            HostScreen = hostScreen;
            _navigationRegionManager = navigationRegionManager;

            ManualSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(manualStrategy))
                .DisposeWith(_disposables);

            QuickSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(quickStrategy))
                .DisposeWith(_disposables);
        }

        private void NavigateToSelection(SchedulingStrategy strategy)
        {
            _navigationRegionManager.NavigateTo<SchedulingWizardViewModel>(RegionIds.Scheduling, strategy);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}