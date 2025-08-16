using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Presentation.TimeTable.Converters
{
    public class TableCellRowConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int row)
            {
                return row * 2 + 1;
            }
            else if (value is ITableCell cell)
            {
                return cell.Row * 2 + 1;
            }
            return 1;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
