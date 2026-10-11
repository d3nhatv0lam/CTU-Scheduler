using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Styling;
using CTUScheduler.AppServices.Services.UserSettingService;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.Presentation.Services.Theme;

public class ThemeService : IThemeService, IDisposable
{
    private readonly IDisposable _disposable;

    public ThemeService(IUserSettingService userSettingService)
    {
        if (Application.Current is null)
        {
            throw new InvalidOperationException("Application.Current is null.");
        }

        LoadTheme(userSettingService.CurrentAppearanceSettings.Theme);

        ThemeChanged = Observable.FromEvent<EventHandler, EventArgs>(
                h => (s, e) => h(e),
                h => Application.Current.ActualThemeVariantChanged += h,
                h => Application.Current.ActualThemeVariantChanged -= h)
            .Select(_ => MapToAppTheme(Application.Current.ActualThemeVariant))
            .Publish()
            .RefCount();

        _disposable = ThemeChanged
            .DistinctUntilChanged()
            .Subscribe(newTheme =>
                userSettingService.UpdateSettings(settings =>
                    settings with
                    {
                        Appearance = settings.Appearance with { Theme = newTheme }
                    }));
    }

    public AppTheme CurrentTheme => MapToAppTheme(Application.Current!.ActualThemeVariant);
    public IObservable<AppTheme> ThemeChanged { get; }

    private void LoadTheme(AppTheme theme)
    {
        Application.Current!.RequestedThemeVariant = MapToAvaloniaTheme(theme);
    }

    private static ThemeVariant MapToAvaloniaTheme(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.System => ThemeVariant.Light,
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Light
        };
    }

    private static AppTheme MapToAppTheme(ThemeVariant variant)
    {
        if (variant == ThemeVariant.Light) return AppTheme.Light;
        if (variant == ThemeVariant.Dark) return AppTheme.Dark;
        return AppTheme.System;
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}