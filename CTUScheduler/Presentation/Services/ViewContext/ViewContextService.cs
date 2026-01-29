using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using DialogHostAvalonia.Utilities;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.ViewContext;

public class ViewContextService: IViewContextService, IDisposable
{
    private readonly BehaviorSubject<TopLevel?> _toplevelSubject = new(null);
    private readonly ILogger<ViewContextService> _logger;
    
    public TopLevel? CurrentTopLevel => _toplevelSubject.Value;
    public IObservable<TopLevel?> WhenTopLevelChanged { get; }

    public ViewContextService(ILogger<ViewContextService> logger)
    {
        _logger = logger;
        WhenTopLevelChanged = _toplevelSubject.AsObservable();
    }
    
    public void SetTopLevel(TopLevel? topLevel)
    {
        _logger.LogDebug("Set TopLevel to {TopLevel}", topLevel?.GetType().Name);
        _toplevelSubject.OnNext(topLevel);
    }

    public void Dispose()
    {
        _toplevelSubject.Dispose();
        _logger.LogInformation("ViewContextService disposed");
    }
}