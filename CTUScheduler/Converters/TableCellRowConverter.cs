using Avalonia.Data.Converters;
using CTUScheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Converters
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
