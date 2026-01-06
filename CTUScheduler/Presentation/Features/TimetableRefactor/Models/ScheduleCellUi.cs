using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Models;

public class ScheduleCellUi: ITableCell
{
    // ReSharper disable once InconsistentNaming
    private static readonly int DEFAULT_ATTENDING_DAY = 2;

    // ReSharper disable once InconsistentNaming
    private static readonly int DEFAULT_START_PERIOD = 1;

    // ReSharper disable once InconsistentNaming
    private static readonly int DEFAULT_NUMBER_OF_PERIODS = 1;

    private readonly ScheduleGroupCellShared _shared;
    public ScheduleGroupCellShared Shared => _shared;
        
    public int Row
    {
        get
        {
            // TietBatDau  <=> row index difference 1 when TietBatDau < 5
            if (StartPeriod < 5) return StartPeriod - 1;
            return StartPeriod;
        }
    }

    /// <summary>
    /// ThuDiHoc = Column index difference 2
    /// </summary>
    public int Column => AttendingDay - 2;

    /// <summary>
    /// Số tiết học
    /// </summary>
    public int RowSpan => NumberOfPeriods;

    public int ColumnSpan { get; set; } = 1;
    public string Room { get; set; } = string.Empty;
    public int AttendingDay { get; set; } = DEFAULT_ATTENDING_DAY;
    public int StartPeriod { get; set; } = DEFAULT_START_PERIOD;
    public int NumberOfPeriods { get; set; } = DEFAULT_NUMBER_OF_PERIODS;

    public ScheduleCellUi(ScheduleGroupCellShared shared)
    {
        _shared = shared;
    }
}