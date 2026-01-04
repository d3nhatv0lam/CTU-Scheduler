using System;
using System.Collections.Generic;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;

public interface ICourseDisplaySource
{
    IObservable<string> Code { get; }
    IObservable<string> Name { get; }
    IObservable<int> Credits { get; }
    IObservable<string> Group { get; }
    IObservable<string> Lecturer { get; }
    IObservable<int> RemainingStudents { get; }
    IObservable<int> TotalStudents { get; }
    IEnumerable<ClassDay> ClassDays { get; } 
}