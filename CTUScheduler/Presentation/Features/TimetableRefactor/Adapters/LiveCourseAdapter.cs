using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;

public class LiveCourseAdapter: ICourseDisplaySource
{
    public LiveCourseAdapter(RuntimeCourse runtimeCourse, CourseSection section)
    {
        Code = runtimeCourse.WhenAnyValue(x => x.Code);
        Name = runtimeCourse.WhenAnyValue(x => x.Name_VN);
        Credits = runtimeCourse.WhenAnyValue(x => x.Credits);
        
        Group = Observable.Return(section.Group); 
        Lecturer = Observable.Return(section.Lecturer);
        
        RemainingStudents = Observable.Return(section.RemainingStudents); 
        TotalStudents = Observable.Return(section.TotalStudents);
        
        ClassDays = section.ClassDays;
    }

    public IObservable<string> Code { get; }
    public IObservable<string> Name { get; }
    public IObservable<int> Credits { get; }
    public IObservable<string> Group { get; }
    public IObservable<string> Lecturer { get; }
    public IObservable<int> RemainingStudents { get; }
    public IObservable<int> TotalStudents { get; }
    public IEnumerable<ClassDay> ClassDays { get; }
}