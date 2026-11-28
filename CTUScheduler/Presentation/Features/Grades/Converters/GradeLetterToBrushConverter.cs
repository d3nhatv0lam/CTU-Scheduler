using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Presentation.Features.Grades.Helpers;

namespace CTUScheduler.Presentation.Features.Grades.Converters;

public sealed class GradeLetterToBrushConverter : IValueConverter
{
    private static readonly Dictionary<GradeLetter, IBrush> BrushCache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var letter = value is GradeLetter gl ? gl : GradeLetter.Unknown;
        if (!BrushCache.TryGetValue(letter, out var brush))
        {
            brush = new SolidColorBrush(Color.Parse(letter.ToColorHex()));
            BrushCache[letter] = brush;
        }

        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
