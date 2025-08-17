using System;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.Presentation.Base;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class SelectionViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public enum SelectionType
        {
            Handmade,
            Quick
        };

        public string? UrlPathSegment => "SelectionViewModel";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit,Unit> HandmadeSelectionCommand { get; protected set; }
        public ReactiveCommand<Unit,Unit> QuickSelectionCommand { get; protected set; }
        
        public SelectionViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            HandmadeSelectionCommand = ReactiveCommand.Create(() => NavigateToSelection(SelectionType.Handmade)).DisposeWith(_disposables);
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
