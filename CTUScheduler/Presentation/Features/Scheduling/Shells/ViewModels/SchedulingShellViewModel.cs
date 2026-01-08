using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Selection.ViewModels;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Scheduling.Shells.ViewModels
{
    public class SchedulingShellViewModel : ViewModelBase, IRoutableViewModel, IDisposable, IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new();
        private string _btnNextContent = "Tiếp theo";
        private IStepViewModel[] _stepsVM = null!;
        private int _currentStepIndex;
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

        public ViewModelActivator Activator { get; } = new();
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
                    .Select(index => _stepsVM[index])
                    .ToProperty(this, nameof(CurrentStep), scheduler: RxApp.MainThreadScheduler)
                    .DisposeWith(_disposables);
            
            CurrentStepIndex = 0;

            this.WhenAnyValue(x => x._stepsVM, x => x.CurrentStepIndex)
                .Subscribe(tuple =>
                {
                    var (steps, index) = tuple;
                    BtnNextContent = index == steps.Length - 1 ? "Hoàn thành" : "Tiếp theo";
                });
            
            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack).DisposeWith(_disposables);
            var canNavigateNext = this.WhenAnyValue(x => x.CurrentStep)
                .Select(currentStep =>
                {
                    if (currentStep is INextStepCondition nextStepCondition)
                        return nextStepCondition.WhenAnyValue(x => x.IsNextStepEnabled);
                    return Observable.Return(true);
                })
                .Switch();
            NavigateNextCommand = ReactiveCommand.CreateFromTask(async () => await NavigateNext(),canNavigateNext)
                .DisposeWith(_disposables);
            
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                Disposable.Create(() =>
                {
                    Dispose();
                    Log.Information("SchedulingShellViewModel: Disposed");
                }).DisposeWith(disposables);
            });
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
            [
                step1,
                step2
            ];
            return steps;
        }

        private async Task NavigateNext()
        {
            await OnNavigate();
            if (CurrentStepIndex == _stepsVM.Length - 1)
            {
                if (HostScreen is ICloseableDialog closeableDialog)
                    closeableDialog.Close();
                else Log.Warning("SchedulingShellViewModel: CurrentStepIndex reach step but can't close dialog");
            }
            else
            if (CurrentStepIndex < _stepsVM.Length - 1)
            {
                CurrentStepIndex++;
            }
            else
            {
                Log.Warning("SchedulingShellViewModel: invalid index step?");
            }
        }

        private async Task NavigateBack()
        {
            await OnNavigate();
            if (CurrentStepIndex == 0)
            {
                HostScreen.Router.NavigateBack.Execute();
            }
            else
                CurrentStepIndex--;
        }

        private async Task OnNavigate()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (CurrentStep is ICleanup cleanup)
                cleanup.Cleanup();
            if (CurrentStep is ICleanupAsync cleanupAsync)
                await cleanupAsync.CleanupAsync();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
