using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Interfaces
{
    public interface ITableCell
    {
        int Row { get;  }
        int Column { get;  }
        int RowSpan { get; }
        int ColumnSpan { get; }
    }
}
