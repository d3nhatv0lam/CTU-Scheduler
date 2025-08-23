using System.Collections.Generic;
using Avalonia.Media;

namespace CTUScheduler.Presentation.Features.TimeTable.Resources;

public static class ColorPalettes
{
    public static IReadOnlyList<IBrush> Colors { get; } =
    [
        new SolidColorBrush(Color.Parse("#ffd1dc")), // hong phan
        new SolidColorBrush(Color.Parse("#add8e6")), // xanh duong nhat
        new SolidColorBrush(Color.Parse("#FFFacd")), // vang nhat chanh
        new SolidColorBrush(Color.Parse("#e29b9a")),
        new SolidColorBrush(Color.Parse("#f5f5dc")),
        new SolidColorBrush(Color.Parse("#7d66ba")),
        new SolidColorBrush(Color.Parse("#d5fad6")), // xanh la bac ha
        new SolidColorBrush(Color.Parse("#B2f2e9")),
        new SolidColorBrush(Color.Parse("#84aae5")),
        new SolidColorBrush(Color.Parse("#8ad485")),
        new SolidColorBrush(Color.Parse("#007ba7")), // xanh da troi
        new SolidColorBrush(Color.Parse("#d1fff4")), // pastel cyan
    ];
}