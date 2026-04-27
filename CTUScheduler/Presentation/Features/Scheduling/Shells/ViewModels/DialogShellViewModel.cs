using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Irihi.Avalonia.Shared.Contracts;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels
{
    public class DialogShellViewModel: ViewModelBase, IScreen, IDisposable, IDialogContext
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IViewportService _viewportService;
        private readonly INavigationRegionManager _navigationRegionManager;
        
        public event EventHandler<object?>? RequestClose;
        public void Close() => RequestClose?.Invoke(this, null);

        public RoutingState Router { get; } = new ();
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; protected set; }
        

        public DialogShellViewModel(IViewportService viewportService, INavigationRegionManager navigationRegionManager)
        {
            _viewportService = viewportService;
            _navigationRegionManager = navigationRegionManager;

            _navigationRegionManager.Register(RegionIds.Scheduling, this)
                .DisposeWith(_disposables);
            
            CloseDialogCommand = ReactiveCommand.Create(Close)
                .DisposeWith(_disposables);

            _navigationRegionManager.NavigateTo<SelectionViewModel>(RegionIds.Scheduling);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }

    }
}
