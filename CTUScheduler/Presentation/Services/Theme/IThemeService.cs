using System;
using System.Reactive;
using CTUScheduler.Core.Models.Settings;

namespace CTUScheduler.Presentation.Services.Theme;

/// <summary>
/// Notify when App theme changed
/// </summary>
public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    IObservable<AppTheme> ThemeChanged { get; } 
}