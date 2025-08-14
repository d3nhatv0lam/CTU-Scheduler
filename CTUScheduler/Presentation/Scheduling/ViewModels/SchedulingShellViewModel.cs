using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Scheduling.ViewModels
{
    public class SchedulingShellViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private string _btnNextContent = "Tiếp theo";
        private IStepViewModel[] stepsVM;
        private int _currentStepIndex = 0;
        private ObservableAsPropertyHelper<IStepViewModel> _currentStep;

        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set => this.RaiseAndSetIfChanged(ref _currentStepIndex, value);
        }
        public IStepViewModel CurrentStep => _currentStep.Value;

        public string? UrlPathSegment => "Find_Course_Shell";
        public IScreen HostScreen { get; }
        public string BtnNextContent
        {
            get => _btnNextContent;
            set => this.RaiseAndSetIfChanged(ref _btnNextContent, value);
        }
        public ReactiveCommand<Unit, Unit> NavigateNextCommand { get; protected set; }
        public ReactiveCommand<Unit, Unit> NavigateBackCommand { get; protected set; }

        public SchedulingShellViewModel()
        {

        }

        public SchedulingShellViewModel(IScreen hostScreen, SelectionViewModel.SelectionType selectionType)
        {
            HostScreen = hostScreen;
            NavigateBackCommand = ReactiveCommand.Create(NavigateBack).DisposeWith(_disposables);
            NavigateNextCommand = ReactiveCommand.Create(NavigateNext).DisposeWith(_disposables);

            InitWizard(selectionType);

            _currentStep = this.WhenAnyValue(x => x.CurrentStepIndex)
                    .Select(index => stepsVM![index])
                    .ToProperty(this, nameof(CurrentStep), scheduler: RxApp.MainThreadScheduler)
                    .DisposeWith(_disposables);

            CurrentStepIndex = 0;

            this.WhenAnyValue(x => x.stepsVM, x => x.CurrentStepIndex).Subscribe(tuple =>
            {
                BtnNextContent = tuple.Item2 == tuple.Item1.Length - 1 ? "Hoàn thành" : "Tiếp theo";
            });
        }

        private void InitWizard(SelectionViewModel.SelectionType selectionType)
        {
            stepsVM = selectionType switch 
            { 
                SelectionViewModel.SelectionType.Handmade => new IStepViewModel[] { new HandmadeFindCourseViewModel(HostScreen), new TimeTableSchedulerViewModel() },
                //SelectionViewModel.SelectionType.Quick => new IStepViewModel[] { new QuickSelectionViewModel(HostScreen) },
                _ => throw new ArgumentOutOfRangeException(nameof(selectionType), selectionType, null)
            };
        }

        private void NavigateNext()
        {
            if (CurrentStepIndex < stepsVM.Length - 1)
            {
                CurrentStepIndex++;
            }
            else
            {
                Log.Warning("SchedulingShellViewModel: invalid index step?");
            }
        }

        private void NavigateBack()
        {
            if (CurrentStepIndex == 0)
                HostScreen.Router.NavigateBack.Execute();
            else
                CurrentStepIndex--;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
