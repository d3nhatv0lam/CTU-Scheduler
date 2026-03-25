using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels
{
    public class SelectionViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public enum SelectionType
        {
            Manual,
            Quick
        }

        public string? UrlPathSegment => "SelectionViewModel";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit,Unit> ManualSelectionCommand { get; protected set; }
        public ReactiveCommand<Unit,Unit> QuickSelectionCommand { get; protected set; }
        
        public SelectionViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            ManualSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(SelectionType.Manual)).DisposeWith(_disposables);
            QuickSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(SelectionType.Quick)).DisposeWith(_disposables);
        }

        private void NavigateToSelection(SelectionType type)
        {
            HostScreen.Router.Navigate.Execute(new SchedulingShellViewModel(HostScreen, type));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
