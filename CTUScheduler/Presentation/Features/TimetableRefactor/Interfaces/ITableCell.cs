namespace CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces
{
    public interface ITableCell
    {
        int Row { get;  }
        int Column { get;  }
        int RowSpan { get; }
        int ColumnSpan { get; }
    }
}
