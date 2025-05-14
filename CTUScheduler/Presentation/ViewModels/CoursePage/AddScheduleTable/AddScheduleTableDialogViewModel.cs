using CTUScheduler.Presentation.ViewModels.Base;
using DialogHostAvalonia;
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
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly string _dialogIdentifier;

        public RoutingState Router { get; } = new RoutingState();
        public ReactiveCommand<Unit, Unit> CloseDialogCommand { get; protected set; }

        public AddScheduleTableDialogViewModel() { }
        public AddScheduleTableDialogViewModel(string dialogIdentifier)
        {
            _dialogIdentifier = dialogIdentifier;
            CloseDialogCommand = ReactiveCommand.Create(CloseDialog).DisposeWith(_disposables);

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
