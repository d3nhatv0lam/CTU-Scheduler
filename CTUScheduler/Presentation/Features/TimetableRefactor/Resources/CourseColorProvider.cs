using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Resources;

public class CourseColorProvider
{
    private static readonly Lazy<IReadOnlyList<IBrush>> Palette = new(() =>
    [
        new ImmutableSolidColorBrush(Color.Parse("#0466c8")), 
        new ImmutableSolidColorBrush(Color.Parse("#5fa8d3")),
        new ImmutableSolidColorBrush(Color.Parse("#0582ca")), 

        new ImmutableSolidColorBrush(Color.Parse("#48C9B0")), 
        new ImmutableSolidColorBrush(Color.Parse("#63A6A0")),
        new ImmutableSolidColorBrush(Color.Parse("#84aec2")), 

        new ImmutableSolidColorBrush(Color.Parse("#64D8D8")), 
        new ImmutableSolidColorBrush(Color.Parse("#43A9B7")), 
        new ImmutableSolidColorBrush(Color.Parse("#88BCC0")), 

        new ImmutableSolidColorBrush(Color.Parse("#7CB9E8")),
        new ImmutableSolidColorBrush(Color.Parse("#5499C7")),
        new ImmutableSolidColorBrush(Color.Parse("#7E90A3")), 
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