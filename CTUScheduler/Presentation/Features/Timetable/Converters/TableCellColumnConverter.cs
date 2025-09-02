using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Presentation.Features.Timetable.Converters
{
    public class TableCellColumnConverter: IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int column)
            {
                return column * 2 + 1;
            }
            else if (value is ITableCell cell)
            {
                return cell.Column * 2 + 1;
            }
            return 1;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
