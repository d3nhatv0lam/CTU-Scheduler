using System;
using System.Reactive.Linq;

namespace CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;

public interface IWizardStep
{
    IObservable<bool> CanNavigateNext => Observable.Return(true);
}