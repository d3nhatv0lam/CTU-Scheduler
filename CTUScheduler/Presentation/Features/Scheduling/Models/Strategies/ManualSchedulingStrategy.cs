using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;

public class ManualSchedulingStrategy : SchedulingStrategy
{
    public override string Name => "Manual Scheduling";

    public ManualSchedulingStrategy(IViewModelFactory factory) : base(factory) {}

    public override IWizardStep[] CreateSteps(SchedulingWizardContext context,
        CompositeDisposable disposables)
    {
        var step1 = Factory.Create<HandmadeFindCourseViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);
        var step2 = Factory.Create<TimetableSchedulerViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);
        
        return [step1, step2];
    }
}