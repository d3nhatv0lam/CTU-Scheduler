using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Resources;

public class CourseColorProvider
{
    private static readonly Lazy<IReadOnlyList<IBrush>> Palette = new(() =>
    [
        new ImmutableSolidColorBrush(Color.Parse("#D4ECF9")), // xanh lam nhạt 
        new ImmutableSolidColorBrush(Color.Parse("#A6DEF5")), // xanh lơ nhạt 
        new ImmutableSolidColorBrush(Color.Parse("#7ED1F1")), // xanh lơ 
        new ImmutableSolidColorBrush(Color.Parse("#B4C5F0")), // xanh lam ánh tím nhạt
        new ImmutableSolidColorBrush(Color.Parse("#A7B9EB")), // xanh periwinkle
        new ImmutableSolidColorBrush(Color.Parse("#859CE0")), // xanh lam xám vừa
        new ImmutableSolidColorBrush(Color.Parse("#6D84CD")), // xanh lam xám
        new ImmutableSolidColorBrush(Color.Parse("#5A6EB0")), // xanh chàm 
        new ImmutableSolidColorBrush(Color.Parse("#415792")), // xanh navy xám 
        new ImmutableSolidColorBrush(Color.Parse("#364B82")), // xanh navy đậm 
        new ImmutableSolidColorBrush(Color.Parse("#2D4075")), // xanh lam biển 
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