using System;
using System.Reactive;

namespace CTUScheduler.Presentation.Services.Theme;

/// <summary>
/// Notify when App theme changed
/// </summary>
public interface IThemeService
{
    IObservable<Unit> ThemeChanged { get; } 
}