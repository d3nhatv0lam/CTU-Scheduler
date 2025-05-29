using Avalonia.Controls.Templates;
using CTUScheduler.AppServices.Services.Implementations;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Presentation.ViewModels.Base;
using DialogHostAvalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable
{
    public class AddScheduleTableDialogViewModel: ViewModelBase, IScreen, IDisposable
    {
        private readonly IViewportService _viewportService;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly string _dialogIdentifier;
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

        public AddScheduleTableDialogViewModel() { }

        public AddScheduleTableDialogViewModel(string dialogIdentifier)
        {
            _dialogIdentifier = dialogIdentifier;
            _viewportService = App.ServiceProvider!.GetRequiredService<IViewportService>();
            CloseDialogCommand = ReactiveCommand.Create(CloseDialog).DisposeWith(_disposables);
            _viewportService.SizeChanged
                .Subscribe(size =>
                {
                    Height = size.Height;
                    Width = size.Width;
                }) .DisposeWith(_disposables);
            Router.Navigate.Execute(new SelectionViewModel(this));
        }

        private void CloseDialog()
        {
            DialogHost.Close(_dialogIdentifier);
        }
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
