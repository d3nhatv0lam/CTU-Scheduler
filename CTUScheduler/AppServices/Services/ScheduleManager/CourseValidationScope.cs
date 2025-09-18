using System;
using System.Collections.Generic;
using System.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;

namespace CTUScheduler.AppServices.Services.ScheduleManager;

public class CourseValidationScope: ICourseValidationScope
{
    private readonly CourseManager _courseManager;
    private bool _isCommited = false;
    
    public CourseValidationScope(CourseManager courseManager)
    {
        _courseManager = courseManager;
    }
    
    public bool Validate(IEnumerable<Course> courses, IEnumerable<ScheduleTable> tables)
    {
        _isCommited = false;

        foreach (var table in tables)
        {
            var x = table.SavedCourseGroupKeys;
        }
        return true;
    }

    public void Commit()
    {
        if (!_isCommited)
            throw new Exception("Validation failed");
        
    }
    
}