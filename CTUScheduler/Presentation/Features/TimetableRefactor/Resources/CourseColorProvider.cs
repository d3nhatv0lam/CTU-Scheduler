using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Resources;

public class CourseColorProvider
{
    private static readonly Lazy<IReadOnlyList<IBrush>> Palette = new(() =>
    [
        new ImmutableSolidColorBrush(Color.Parse("#ffd1dc")), 
        new ImmutableSolidColorBrush(Color.Parse("#add8e6")), 
        new ImmutableSolidColorBrush(Color.Parse("#FFFacd")), 
        new ImmutableSolidColorBrush(Color.Parse("#e29b9a")),
        new ImmutableSolidColorBrush(Color.Parse("#7d66ba")),
        new ImmutableSolidColorBrush(Color.Parse("#d5fad6")), 
        new ImmutableSolidColorBrush(Color.Parse("#B2f2e9")),
        new ImmutableSolidColorBrush(Color.Parse("#84aae5")),
        new ImmutableSolidColorBrush(Color.Parse("#8ad485")),
        new ImmutableSolidColorBrush(Color.Parse("#007ba7")), 
        new ImmutableSolidColorBrush(Color.Parse("#d1fff4")),
    ]);
    
    private readonly Dictionary<string, IBrush> _assignedColors = new();
    
    public IBrush GetColorForCourse(string courseCode)
    {
        if (_assignedColors.TryGetValue(courseCode, out var color)) return color;
        var palette = Palette.Value;
        int nextIndex = _assignedColors.Count % palette.Count;
        var newColor = palette[nextIndex];
        _assignedColors[courseCode] = newColor;
        return newColor;
    }
    public void Reset()
    {
        _assignedColors.Clear();
    }
}