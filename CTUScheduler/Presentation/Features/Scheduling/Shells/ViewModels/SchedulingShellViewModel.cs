using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels
{
    public class SchedulingShellViewModel : ViewModelBase, IRoutableViewModel, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private string _btnNextContent = "Tiếp theo";
        private IStepViewModel[] _stepsVM;
        private int _currentStepIndex = 0;
        private readonly ObservableAsPropertyHelper<IStepViewModel> _currentStep;

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
            InitWizard(selectionType);

            _currentStep = this.WhenAnyValue(x => x.CurrentStepIndex)
                    .Select(index => _stepsVM![index])
                    .ToProperty(this, nameof(CurrentStep), scheduler: RxApp.MainThreadScheduler)
                    .DisposeWith(_disposables);
            
            CurrentStepIndex = 0;

            this.WhenAnyValue(x => x._stepsVM, x => x.CurrentStepIndex)
                .Subscribe(tuple =>
                {
                    var (steps, index) = tuple;
                    BtnNextContent = index == steps.Length - 1 ? "Hoàn thành" : "Tiếp theo";
                });
            
            NavigateBackCommand = ReactiveCommand.Create(NavigateBack).DisposeWith(_disposables);
            var canNavigateNext = this.WhenAnyValue(x => x.CurrentStep)
                .Select(currentStep =>
                {
                    if (currentStep is INextStepCondition nextStepCondition)
                        return nextStepCondition.WhenAnyValue(x => x.IsNextStepEnabled);
                    return Observable.Return(true);
                })
                .Switch();
            NavigateNextCommand = ReactiveCommand.Create(NavigateNext,canNavigateNext).DisposeWith(_disposables);
        }

        private void InitWizard(SelectionViewModel.SelectionType selectionType)
        {
            _stepsVM = selectionType switch 
            { 
                SelectionViewModel.SelectionType.Manual => CreateManualSteps(),
                //SelectionViewModel.SelectionType.Quick => new IStepViewModel[] { new QuickSelectionViewModel() },
                _ => throw new ArgumentOutOfRangeException(nameof(selectionType), selectionType, null)
            };
        }

        private IStepViewModel[] CreateManualSteps()
        {
            var step1 = new HandmadeFindCourseViewModel();
            var step2 = new TimetableSchedulerViewModel(step1.CoursesSourceList);
            _disposables.Add(step1);
            _disposables.Add(step2);
            
            IStepViewModel[] steps =
            {
                step1,
                step2
            };
            return steps;
        }

        private void NavigateNext()
        {
            if (CurrentStepIndex < _stepsVM.Length - 1)
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
            {
                HostScreen.Router.NavigateBack.Execute();
                Dispose();
            }
            else
                CurrentStepIndex--;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
