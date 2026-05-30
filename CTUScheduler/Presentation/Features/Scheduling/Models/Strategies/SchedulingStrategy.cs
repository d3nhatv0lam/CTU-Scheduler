using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;

public abstract class SchedulingStrategy
{
    protected readonly IViewModelFactory Factory;

    public abstract string Name { get; }
    public virtual int StartStepIndex => 0;

    protected SchedulingStrategy(IViewModelFactory factory)
    {
        Factory = factory;
    }

    public abstract Task<IWizardStep[]> CreateStepsAsync(SchedulingWizardContext context,
        CompositeDisposable disposables,
        CancellationToken cancellationToken = default);
}