namespace CTUScheduler.Core.Interfaces
{
    public interface ITableCell
    {
        int Row { get;  }
        int Column { get;  }
        int RowSpan { get; }
        int ColumnSpan { get; }
    }
}
