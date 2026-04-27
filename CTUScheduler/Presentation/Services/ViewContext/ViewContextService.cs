using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.ViewContext;

public class ViewContextService: IViewContextService, IDisposable
{
    private readonly BehaviorSubject<TopLevel?> _toplevelSubject = new(null);
    private readonly ILogger<ViewContextService> _logger;
    private bool _isDisposed;
    
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
        if (_isDisposed) return;
        _toplevelSubject.Dispose();
        _logger.LogInformation("ViewContextService disposed");
        _isDisposed = true;
    }
}