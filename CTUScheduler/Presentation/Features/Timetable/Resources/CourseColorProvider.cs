using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace CTUScheduler.Presentation.Features.Timetable.Resources;

public class CourseColorProvider
{
    private readonly Dictionary<string, IBrush> _assignedColors = new();

    private readonly List<IBrush> _palette = new() 
    { 
        Brushes.CornflowerBlue, Brushes.Coral, Brushes.MediumSeaGreen, 
        Brushes.Goldenrod, Brushes.Orchid, Brushes.SlateGray
    }; 

    public IBrush GetColorForCourse(string courseCode)
    {
        if (_assignedColors.TryGetValue(courseCode, out var color)) return color;
        
        int index = Math.Abs(courseCode.GetHashCode()) % _palette.Count;
        var newColor = _palette[index];
        
        _assignedColors[courseCode] = newColor;
        return newColor;
    }
}