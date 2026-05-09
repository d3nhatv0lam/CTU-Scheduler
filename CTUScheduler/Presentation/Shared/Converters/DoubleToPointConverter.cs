using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace CTUScheduler.Presentation.Shared.Converters;

public class DoubleToPointConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double y && parameter is string param)
        {
            var parts = param.Split(',');
            if (parts.Length == 2)
            {
                return new Point(double.Parse(parts[0]), y);
            }
            else if (parts.Length == 1)
            {
                return new Point(double.Parse(parts[0]), y);
            }
        }
        return new Point(0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}