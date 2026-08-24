using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Styling;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.Presentation.Services.Theme;

public class ThemeService : IThemeService
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
            .Select(_ => MapToAppTheme(Application.Current.ActualThemeVariant))
            .Publish()
            .RefCount();
    }

    public AppTheme CurrentTheme => MapToAppTheme(Application.Current!.ActualThemeVariant);
    public IObservable<AppTheme> ThemeChanged { get; }

    private static AppTheme MapToAppTheme(ThemeVariant variant)
    {
        if (variant == ThemeVariant.Light) return AppTheme.Light;
        if (variant == ThemeVariant.Dark) return AppTheme.Dark;
        return AppTheme.System;
    }
}