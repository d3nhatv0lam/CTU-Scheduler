using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable
{
    public class SelectionViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();  
        public enum SelectionType
        {
            Handmade,
            Quick,
            UserData
        }

        public string? UrlPathSegment => "SelectionViewModel";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit,Unit> HandmadeSelectionCommand { get; protected set; }
        public ReactiveCommand<Unit,Unit> QuickSelectionCommand { get; protected set; }
        //public ReactiveCommand<Unit, Unit> UserDataSelectionCommand { get; protected set; }
        public SelectionViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
         
            HandmadeSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(SelectionType.Handmade)).DisposeWith(_disposables);
            QuickSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(SelectionType.Quick)).DisposeWith(_disposables);
        }

        private void NavigateToSelection(SelectionType type)
        {
            HostScreen.Router.Navigate.Execute(new AddCourseShellViewModel(HostScreen, type));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
