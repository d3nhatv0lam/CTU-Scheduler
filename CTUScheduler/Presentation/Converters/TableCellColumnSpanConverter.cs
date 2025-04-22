using Avalonia.Data.Converters;
using CTUScheduler.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.Converters
{
    public class TableCellColumnSpanConverter: IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int columnSpan)
            {
                return columnSpan * 2 - 1;
            }
            else if (value is ITableCell cell)
            {
                return cell.ColumnSpan * 2 - 1;
            }
            return 1;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
