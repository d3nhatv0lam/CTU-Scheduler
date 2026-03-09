using System;
using Avalonia.Controls;

namespace CTUScheduler.Presentation.Services.ViewContext.Interfaces;

public interface IViewContextService
{
    TopLevel? CurrentTopLevel { get; }
    
    IObservable<TopLevel?> WhenTopLevelChanged { get; }
    
    void SetTopLevel(TopLevel? topLevel);
}