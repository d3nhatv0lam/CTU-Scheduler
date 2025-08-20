using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Extensions;

public static class ScheduleTableExtension
{
    public static void AddToTable(this ScheduleTable scheduleTable, SectionChoice sectionChoice)
    {
        var scheduleCells = sectionChoice.ToScheduleCells();
        
    }
}