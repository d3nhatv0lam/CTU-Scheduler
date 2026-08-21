using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;

namespace CTUScheduler.Presentation.Services.Theme;


public class ThemeService: IThemeService
{
    public ThemeService()
    {
        if (Application.Current is null)
        {
            throw new InvalidOperationException("Application.Current is null.");
        }

        ThemeChanged = Observable.FromEvent<EventHandler, EventArgs>(
                h => (s, e) => h(e),
                h => Application.Current.ActualThemeVariantChanged += h,
                h => Application.Current.ActualThemeVariantChanged -= h)
            .Select(_ => Unit.Default)
            .Publish()
            .RefCount();
    }
    
    public IObservable<Unit> ThemeChanged { get; }
}