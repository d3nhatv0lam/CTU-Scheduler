using System;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Viewport;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels
{
    public class DialogShellViewModel: ViewModelBase, IScreen, IDisposable, ICloseableDialog
    {
        private readonly IViewportService _viewportService;
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
            RequestClose?.Invoke(null);
        }

        public RoutingState Router { get; } = new ();
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; protected set; }
        

        public DialogShellViewModel()
        {
            _viewportService = App.ServiceProvider!.GetRequiredService<IViewportService>();
            
            CloseDialogCommand = ReactiveCommand.Create(() => Close())
                .DisposeWith(_disposables);
            _viewportService.SizeChanged
                .Subscribe(size =>
                {
                    Height = size.Height;
                    Width = size.Width;
                }) .DisposeWith(_disposables);
            
            Router.Navigate.Execute(new SelectionViewModel(this));
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }

    }
}
