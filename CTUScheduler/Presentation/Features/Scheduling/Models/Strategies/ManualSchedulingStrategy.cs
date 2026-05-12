using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;

public class ManualSchedulingStrategy : SchedulingStrategy
{
    public override string Name => nameof(ManualSchedulingStrategy);

    public ManualSchedulingStrategy(IViewModelFactory factory) : base(factory)
    {
    }

    public override Task<IWizardStep[]> CreateStepsAsync(SchedulingWizardContext context,
        CompositeDisposable disposables)
    {
        var step1 = Factory.Create<FindCourseViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);
        var step2 = Factory.Create<TimetableSchedulerViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);

        IWizardStep[] result = [step1, step2];
        return Task.FromResult(result);
    }
}