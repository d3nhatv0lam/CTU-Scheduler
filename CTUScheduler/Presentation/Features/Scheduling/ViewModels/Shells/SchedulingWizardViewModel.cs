using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using Irihi.Avalonia.Shared.Contracts;
using ReactiveUI;
using Serilog;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells
{
    public class SchedulingWizardViewModel : ViewModelBase,
        IRoutableViewModel,
        IDisposable,
        IActivatableViewModel,
        INeedArgs<SchedulingStrategy>
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly IWizardStep[] _stepsVM;
        private readonly ObservableAsPropertyHelper<IWizardStep> _currentStep;
        private string _btnNextContent = "Tiếp theo";
        private int _currentStepIndex;


        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set => this.RaiseAndSetIfChanged(ref _currentStepIndex, value);
        }

        public IWizardStep CurrentWizardStep => _currentStep.Value;

        public string? UrlPathSegment => nameof(SchedulingWizardViewModel);
        public IScreen HostScreen { get; }

        public string BtnNextContent
        {
            get => _btnNextContent;
            set => this.RaiseAndSetIfChanged(ref _btnNextContent, value);
        }

        public ViewModelActivator Activator { get; } = new();
        public ReactiveCommand<Unit, Unit> NavigateNextCommand { get; protected set; }
        public ReactiveCommand<Unit, Unit> NavigateBackCommand { get; protected set; }

        public SchedulingWizardViewModel()
        {
        }

        public SchedulingWizardViewModel(IScreen hostScreen, SchedulingStrategy strategy)
        {
            HostScreen = hostScreen;

            SchedulingWizardContext context = new();

            _stepsVM = strategy.CreateSteps(context, _disposables);

            _currentStep = this.WhenAnyValue(x => x.CurrentStepIndex)
                .Select(index => _stepsVM[index])
                .ToProperty(this, nameof(CurrentWizardStep), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            CurrentStepIndex = 0;

            this.WhenAnyValue(x => x._stepsVM, x => x.CurrentStepIndex)
                .Subscribe(tuple =>
                {
                    var (steps, index) = tuple;
                    BtnNextContent = index == steps.Length - 1 ? "Hoàn thành" : "Tiếp theo";
                })
                .DisposeWith(_disposables);

            NavigateBackCommand = ReactiveCommand.Create(NavigateBack)
                .DisposeWith(_disposables);
            
            var canNavigateNext = this.WhenAnyValue(x => x.CurrentWizardStep)
                .Select(step => step.CanNavigateNext)
                .Switch();
            NavigateNextCommand = ReactiveCommand.Create(NavigateNext, canNavigateNext)
                .DisposeWith(_disposables);

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                Disposable.Create(() =>
                {
                    Dispose();
                    Log.Debug("SchedulingWizardViewModel: Disposed");
                }).DisposeWith(disposables);
            });
        }
        
        private void NavigateNext()
        {
            if (CurrentStepIndex == _stepsVM.Length - 1)
            {
                if (HostScreen is IDialogContext closeableDialog)
                {
                    if (CurrentWizardStep is ICleanup cleanup)
                        cleanup.Cleanup();
                    closeableDialog.Close();
                }
                Log.Warning("SchedulingWizardViewModel: CurrentStepIndex reach step but can't close dialog");
            }
            else if (CurrentStepIndex < _stepsVM.Length - 1)
            {
                CurrentStepIndex++;
            }
            else
            {
                Log.Warning("SchedulingWizardViewModel: invalid index step?");
            }
        }

        private void NavigateBack()
        {
            if (CurrentStepIndex == 0)
            {
                HostScreen.Router.NavigateBack.Execute();
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