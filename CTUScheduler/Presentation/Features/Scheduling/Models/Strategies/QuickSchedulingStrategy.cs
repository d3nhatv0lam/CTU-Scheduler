using System.Reactive.Disposables;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;

public class QuickSchedulingStrategy: SchedulingStrategy
{
    public override string Name => "Quick Scheduling";

    public QuickSchedulingStrategy(IViewModelFactory factory) :base(factory) {}
    
    public override IWizardStep[] CreateSteps(SchedulingWizardContext context, CompositeDisposable disposables)
    {
        throw new System.NotImplementedException();
    }
}