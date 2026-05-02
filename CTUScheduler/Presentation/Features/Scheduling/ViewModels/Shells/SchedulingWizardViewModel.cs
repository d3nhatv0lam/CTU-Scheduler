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
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Shells;

public partial class SchedulingWizardViewModel : ViewModelBase,
    IRoutableViewModel,
    IDisposable,
    IHaveCloseInteraction<Unit>,
    INeedArgs<SchedulingStrategy>
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IUserInteractionService _userInteractionService;
    private readonly ILogger<SchedulingWizardViewModel> _logger;
    private readonly IWizardStep[] _stepsVM;


    [ObservableAsProperty] private IWizardStep _currentWizardStep = null!;
    [ObservableAsProperty] private string _btnNextContent = null!;
    [Reactive] private int _currentStepIndex = 0;


    public string UrlPathSegment => nameof(SchedulingWizardViewModel);
    public IScreen HostScreen { get; }
    public ReactiveCommand<Unit, Unit> NavigateNextCommand { get; }
    public ReactiveCommand<Unit, Unit> NavigateBackCommand { get; }
    public Interaction<Unit, Unit> CloseInteraction { get; } = new();


    public SchedulingWizardViewModel(IScreen hostScreen, SchedulingStrategy strategy,
        IUserInteractionService userInteractionService, ILogger<SchedulingWizardViewModel> logger)
    {
        HostScreen = hostScreen;
        _userInteractionService = userInteractionService;
        _logger = logger;

        SchedulingWizardContext context = new SchedulingWizardContext()
            .DisposeWith(_disposables);
        _stepsVM = strategy.CreateSteps(context, _disposables);

        ArgumentOutOfRangeException.ThrowIfLessThan(_stepsVM.Length, 1, nameof(_stepsVM));

        _currentWizardStepHelper = this.WhenAnyValue(x => x.CurrentStepIndex)
            .Select(index => _stepsVM[index])
            .ToProperty(this,
                nameof(CurrentWizardStep),
                initialValue: _stepsVM[0],
                deferSubscription: false)
            .DisposeWith(_disposables);

        _btnNextContentHelper = this.WhenAnyValue(x => x.CurrentStepIndex)
            .Select(index => index + 1 < _stepsVM.Length ? "Tiếp theo" : "Hoàn thành")
            .ToProperty(this,
                nameof(BtnNextContent),
                initialValue: _stepsVM.Length > 1 ? "Tiếp theo" : "Hoàn thành")
            .DisposeWith(_disposables);

        NavigateBackCommand = ReactiveCommand.Create(NavigateBack)
            .DisposeWith(_disposables);

        var canNavigateNext = this.WhenAnyValue(x => x.CurrentWizardStep)
            .Select(step => step.CanNavigateNext)
            .Switch();
        NavigateNextCommand = ReactiveCommand.CreateFromTask(async () => await NavigateNext(), canNavigateNext)
            .DisposeWith(_disposables);
    }

    private async Task NavigateNext()
    {
        if (CurrentStepIndex == _stepsVM.Length - 1)
        {
            if (CurrentWizardStep is IFinishableStep finishableStep)
                finishableStep.Commit();
            //close dialog
            await CloseInteraction.Handle(Unit.Default);
            _userInteractionService.Toast.Light.Success("Lưu thời khóa biểu thành công!");
        }
        else if (CurrentStepIndex < _stepsVM.Length - 1)
        {
            CurrentStepIndex++;
        }
        else
        {
            _logger.LogWarning("SchedulingWizardViewModel: invalid index step?");
        }
    }

    private void NavigateBack()
    {
        if (CurrentStepIndex <= 0)
        {
            HostScreen.Router.NavigateBack.Execute();
        }
        else
            CurrentStepIndex--;
    }


    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("{this}: Disposed", nameof(SchedulingWizardViewModel));
    }
}