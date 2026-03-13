using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.Core.Extensions;

public static class ClassDayExtension
{
    extension(ClassDay classDay)
    {
        public TimeOfDay TimeOfDay
        {
            get
            {
                if (classDay.StartPeriod <= 5) return TimeOfDay.Morning;
                if (classDay.StartPeriod <= 10) return TimeOfDay.Afternoon;
                return TimeOfDay.Evening;
            }
        }
    }
}