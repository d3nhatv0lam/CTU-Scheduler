using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;

public class StaticCourseAdapter: ICourseDisplaySource
{
    public StaticCourseAdapter(Course courseDto, CourseSection sectionDto)
    {
        Code = Observable.Return(courseDto.Code);
        Name = Observable.Return(courseDto.Name_VN);
        Credits = Observable.Return(courseDto.Credits);
        Group = Observable.Return(sectionDto.Group);
        Lecturer = Observable.Return(sectionDto.Lecturer);
        RemainingStudents = Observable.Return(sectionDto.RemainingStudents);
        TotalStudents = Observable.Return(sectionDto.TotalStudents);
        ClassDays = sectionDto.ClassDays;
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