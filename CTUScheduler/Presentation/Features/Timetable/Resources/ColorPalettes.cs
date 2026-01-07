using System;
using System.Collections.Generic;
using Avalonia.Media;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.Resources;

public static class ColorPalettes
{
    private static readonly Lazy<IReadOnlyList<IBrush>> _colors = new(() =>
    [
        new SolidColorBrush(Color.Parse("#ffd1dc")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#add8e6")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#FFFacd")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#e29b9a")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#7d66ba")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#d5fad6")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#B2f2e9")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#84aae5")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#8ad485")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#007ba7")).ToImmutable(),
        new SolidColorBrush(Color.Parse("#d1fff4")).ToImmutable(),
    ]);
    public static IReadOnlyList<IBrush> Colors => _colors.Value;
}