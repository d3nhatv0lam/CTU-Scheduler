using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using CTUScheduler.Presentation.Services.Navigation;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;

public partial class SchedulingWizardViewModel : ViewModelBase,
    IRoutableViewModel,
    IDisposable,
    IActivatableViewModel,
    IHaveCloseInteraction<Unit>,
    INeedArgs<SchedulingStrategy>
{
    private readonly CompositeDisposable _disposables = new();
    private readonly INavigationRegionManager _navigationRegionManager;
    private readonly IUserInteractionService _userInteractionService;
    private readonly ILogger<SchedulingWizardViewModel> _logger;

    [ObservableAsProperty] private IWizardStep[] _stepsVM = [];
    [ObservableAsProperty] private IWizardStep? _currentWizardStep;
    [ObservableAsProperty] private string _btnNextContent = "Tiếp theo";
    [Reactive] private int _currentStepIndex;

    public string UrlPathSegment => nameof(SchedulingWizardViewModel);
    public IScreen HostScreen { get; }
    public IObservable<bool> IsLoading => LoadStepsCommand.IsExecuting;
    public ReactiveCommand<Unit, Unit> NavigateNextCommand { get; }
    public ReactiveCommand<Unit, Unit> NavigateBackCommand { get; }
    public ReactiveCommand<Unit, IWizardStep[]> LoadStepsCommand { get; }
    public Interaction<Unit, Unit> CloseInteraction { get; } = new();
    public ViewModelActivator Activator { get; } = new();


    public SchedulingWizardViewModel(IScreen hostScreen,
        SchedulingStrategy strategy,
        INavigationRegionManager regionManager,
        IUserInteractionService userInteractionService,
        ILogger<SchedulingWizardViewModel> logger)
    {
        HostScreen = hostScreen;
        _navigationRegionManager = regionManager;
        _userInteractionService = userInteractionService;
        _logger = logger;

        SchedulingWizardContext context = new SchedulingWizardContext()
            .DisposeWith(_disposables);

        // 1. Setup Step Loading Logic
        LoadStepsCommand = ReactiveCommand.CreateFromTask(() => strategy.CreateStepsAsync(context, _disposables))
            .DisposeWith(_disposables);

        _stepsVMHelper = LoadStepsCommand
            .ToProperty(this, x => x.StepsVM, initialValue: [])
            .DisposeWith(_disposables);

        LoadStepsCommand.ThrownExceptions
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(ex =>
            {
                _logger.LogError(ex, "Failed to load scheduling steps");
                _userInteractionService.Toast.Light.Error("Không thể tải dữ liệu lập lịch. Vui lòng thử lại!");
            })
            .DisposeWith(_disposables);


        _currentWizardStepHelper = this.WhenAnyValue(x => x.CurrentStepIndex, x => x.StepsVM)
            .Select(tuple =>
            {
                var (stepIndex, stepsVM) = tuple;
                return stepsVM.Length > stepIndex ? stepsVM[stepIndex] : null;
            })
            .ToProperty(this, nameof(CurrentWizardStep))
            .DisposeWith(_disposables);

        _btnNextContentHelper = this.WhenAnyValue(x => x.CurrentStepIndex, x => x.StepsVM)
            .Select(tuple =>
            {
                if (tuple.Item2.Length == 0) return "...";
                return tuple.Item1 + 1 < tuple.Item2.Length ? "Tiếp theo" : "Hoàn thành";
            })
            .ToProperty(this, nameof(BtnNextContent), initialValue: "...")
            .DisposeWith(_disposables);


        NavigateBackCommand = ReactiveCommand.Create(NavigateBack)
            .DisposeWith(_disposables);

        var canNavigateNext = this.WhenAnyValue(x => x.CurrentWizardStep)
            .Select(step => step?.CanNavigateNext ?? Observable.Return(false))
            .Switch()
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        NavigateNextCommand = ReactiveCommand.CreateFromTask(NavigateNext, canNavigateNext)
            .DisposeWith(_disposables);


        this.WhenActivated(disposables =>
        {
            if (StepsVM.Length == 0 || context.CourseBlueprints.Count == 0)
            {
                LoadStepsCommand.Execute()
                    .ObserveOn(RxSchedulers.MainThreadScheduler)
                    .Subscribe(_ => CurrentStepIndex = strategy.StartStepIndex)
                    .DisposeWith(disposables);
            }
        });
    }

    private async Task NavigateNext()
    {
        if (StepsVM.Length == 0) return;

        if (CurrentStepIndex == StepsVM.Length - 1)
        {
            if (CurrentWizardStep is IFinishableStep finishableStep)
                finishableStep.Commit();

            await CloseInteraction.Handle(Unit.Default);
            _userInteractionService.Toast.Light.Success("Lưu thời khóa biểu thành công!");
        }
        else if (CurrentStepIndex < StepsVM.Length - 1)
        {
            CurrentStepIndex++;
        }
        else
        {
            _logger.LogWarning("{ViewModel}: invalid index step?", nameof(SchedulingWizardViewModel));
        }
    }

    private void NavigateBack()
    {
        if (CurrentStepIndex <= 0)
        {
            _navigationRegionManager.NavigateAndResetTo<SelectionViewModel>(RegionIds.Scheduling);
        }
        else
            CurrentStepIndex--;
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("{ViewModel}: Disposed", nameof(SchedulingWizardViewModel));
    }
}