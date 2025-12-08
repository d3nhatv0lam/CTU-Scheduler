using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CTUScheduler.Presentation.Shells.AppShell.Views;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Presentation.Services.AppToplevel;

public class ToplevelService: IToplevelService, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<ToplevelService> _logger;
    private readonly BehaviorSubject<TopLevel?> _toplevelSubject = new(null);
    
    public IObservable<TopLevel?> ToplevelChanges => _toplevelSubject;

    public ToplevelService(ILogger<ToplevelService> logger)
    {
        _logger = logger;
    }

    public void ShowWindow(Window window)
    {
        var mainWindow = _toplevelSubject.Value;
        if (mainWindow is not Window owner) return;
        
        window.Show(owner);
    }

    public void Initialize(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        // loaded
        if (root.GetVisualRoot() is TopLevel tl)
            RegisterTopLevel(tl);
        // wait for loaded
        else
        {
            root.GetObservable(Control.LoadedEvent)
                .Take(1)
                .Subscribe(_ =>
                {
                    var topLevel = TopLevel.GetTopLevel(root);
                    if (topLevel != null)
                        RegisterTopLevel(topLevel);
                })
                .DisposeWith(_disposables);
        }
        // unloaded
        root.GetObservable(Control.UnloadedEvent)
            .Take(1)
            .Subscribe(_ => UnRegisterTopLevel())
            .DisposeWith(_disposables);
    }

    private void RegisterTopLevel(TopLevel toplevel)
    {
        _toplevelSubject.OnNext(toplevel);
        _logger.LogInformation("Toplevel initialized");
    }

    private void UnRegisterTopLevel()
    {
        _toplevelSubject.OnNext(null);
        _logger.LogInformation("Toplevel uninitialized");
    }

    public void Dispose()
    {
        _toplevelSubject.Dispose();
        _disposables.Dispose();
    }
}