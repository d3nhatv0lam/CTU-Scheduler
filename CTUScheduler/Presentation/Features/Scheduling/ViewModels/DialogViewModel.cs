using System;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.AppServices.Services.Viewport;
using CTUScheduler.Presentation.Base;
using DialogHostAvalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class DialogViewModel: ViewModelBase, IScreen, IDisposable
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


        public RoutingState Router { get; } = new RoutingState();
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; protected set; }

        public DialogViewModel() { }

        public DialogViewModel(DialogHostService.DialogIdentifier dialogIdentifier)
        {
            _viewportService = App.ServiceProvider!.GetRequiredService<IViewportService>();
            
            CloseDialogCommand = ReactiveCommand.Create(() => DialogHost.Close(dialogIdentifier.ToString()))
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
