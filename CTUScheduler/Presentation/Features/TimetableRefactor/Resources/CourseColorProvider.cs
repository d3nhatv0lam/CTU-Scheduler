using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Resources;

public class CourseColorProvider
{
    private static readonly Lazy<IReadOnlyList<IBrush>> Palette = new(() =>
    [
        new ImmutableSolidColorBrush(Color.Parse("#ffe8ed")), 
        new ImmutableSolidColorBrush(Color.Parse("#d6ecf3")), 
        new ImmutableSolidColorBrush(Color.Parse("#FFFBD9")), 
        new ImmutableSolidColorBrush(Color.Parse("#F1CECD")),
        new ImmutableSolidColorBrush(Color.Parse("#e2a8c6")),
        new ImmutableSolidColorBrush(Color.Parse("#eafdeb")), 
        new ImmutableSolidColorBrush(Color.Parse("#daf9f4")),
        new ImmutableSolidColorBrush(Color.Parse("#c1d5f3")),
        new ImmutableSolidColorBrush(Color.Parse("#c6eac2")),
        new ImmutableSolidColorBrush(Color.Parse("#e3f5e0")), 
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