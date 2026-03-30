using System.Reactive.Disposables;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public abstract class SchedulingStrategy
{
    protected readonly IViewModelFactory Factory;
    
    public abstract string Name { get; }

    protected SchedulingStrategy(IViewModelFactory factory)
    {
        Factory = factory;
    }
    
    public abstract IWizardStep[] CreateSteps(SchedulingWizardContext context, CompositeDisposable disposables);
}