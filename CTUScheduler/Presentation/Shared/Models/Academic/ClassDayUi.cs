using System.Linq;

namespace CTUScheduler.Presentation.Shared.Models.Academic;

public class ClassDayUi
{
    public int AttendingDay { get; set; }
    public string Period { get; set; }
    public string Room { get; set; }

    public int StartPeriod() => Period.Trim('-').First() - '0';
    public int PeriodCount() => Period.Trim('-').Length;
    public int EndPeriod() => Period.Trim('-').Last() - '0';

    public string DisplayText => $"{AttendingDay} - {Period} - {Room}";
}