using System.Reactive.Disposables;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Services.Factories;

namespace CTUScheduler.Presentation.Features.Scheduling.Models;

public class QuickSchedulingStrategy: SchedulingStrategy
{
    private readonly IViewModelFactory _factory;
    public override string Name => "Quick Scheduling";

    public QuickSchedulingStrategy(IViewModelFactory factory) :base(factory) {}
    
    public override IWizardStep[] CreateSteps(SchedulingWizardContext context, CompositeDisposable disposables)
    {
        throw new System.NotImplementedException();
    }
}