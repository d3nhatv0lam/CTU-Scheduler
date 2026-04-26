using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.Viewport;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels
{
    public class DialogShellViewModel: ViewModelBase, IScreen, IDisposable, ICloseableDialog
    {
        private readonly IViewportService _viewportService;
        private readonly INavigationRegionManager _navigationRegionManager;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private double _height;
        private double _width;

        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        public double Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }
        public event Action<object?>? RequestClose;
        public void Close(object? result = null)
        {
            RequestClose?.Invoke(result);
        }

        public RoutingState Router { get; } = new ();
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; protected set; }
        

        public DialogShellViewModel(IViewportService viewportService, INavigationRegionManager navigationRegionManager)
        {
            _viewportService = viewportService;
            _navigationRegionManager = navigationRegionManager;

            _navigationRegionManager.Register(RegionIds.Scheduling, this)
                .DisposeWith(_disposables);
            
            CloseDialogCommand = ReactiveCommand.Create(() => Close())
                .DisposeWith(_disposables);
            _viewportService.WhenSizeChanged
                .Subscribe(size =>
                {
                    Height = size.Height;
                    Width = size.Width;
                }) .DisposeWith(_disposables);

            _navigationRegionManager.NavigateTo<SelectionViewModel>(RegionIds.Scheduling);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }

    }
}
