using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class TimeTableSchedulerViewModel: ViewModelBase, IStepViewModel
    {
        private ObservableCollection<SchedulingCourse> _schedulingCourses = new();
        public ObservableCollection<SchedulingCourse> SchedulingCourses => _schedulingCourses;


        public TimeTableSchedulerViewModel()
        {
         
        }

        public void ToSchedulingCourses(ObservableCollection<Course> courses)
        {
            if (SchedulingCourses.Any())
                SchedulingCourses.Clear();
            
            foreach (var course in courses)
            {
                var schedulingCourse = new SchedulingCourse(course, new ObservableCollection<Course>(courses.Where(x => x.Code != course.Code)));
                SchedulingCourses.Add(schedulingCourse);
            }
        }
    }
}
