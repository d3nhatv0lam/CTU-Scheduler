using CTUScheduler.Presentation.ViewModels.Base;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable
{
    public class AddCourseShellViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private string _btnNextContent = "Tiếp theo";

        public string? UrlPathSegment => "Find_Course_Shell";
        public IScreen HostScreen { get; }
        public string BtnNextContent
        {
            get => _btnNextContent;
            set => this.RaiseAndSetIfChanged(ref _btnNextContent, value);
        }

        public ReactiveCommand<Unit, Unit> NavigateNextCourseCommand { get; protected set; }
        public ReactiveCommand<Unit, Unit> NavigateBackCommand { get; protected set; }

        public AddCourseShellViewModel()
        {

        }

        public AddCourseShellViewModel(IScreen hostScreen, SelectionViewModel.SelectionType selectionType)
        {
            HostScreen = hostScreen;
            //NavigateNextCourseCommand = ReactiveCommand.Create(NavigateToNextCourse);
            NavigateBackCommand = ReactiveCommand.Create(NavigateBack).DisposeWith(_disposables);
        }
        private void NavigateBack()
        {
            HostScreen.Router.NavigateBack.Execute();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
