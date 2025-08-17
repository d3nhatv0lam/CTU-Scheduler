using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class TimeTableSchedulerViewModel: ViewModelBase, IStepViewModel, IDisposable , IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly SchedulingCourseViewModel _schedulingCourseVM;
        private readonly SourceList<Course> _coursesSourceList;
        private ReadOnlyObservableCollection<Course> _courseBindable;
        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public ReadOnlyObservableCollection<Course> Courses => _courseBindable;
        public SchedulingCourseViewModel SchedulingCourseVM => _schedulingCourseVM;

        public TimeTableSchedulerViewModel(SourceList<Course> courses)
        {
            _coursesSourceList = courses;
            _schedulingCourseVM = new SchedulingCourseViewModel();

            _coursesSourceList.Connect()
                .Bind(out _courseBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            this.WhenActivated((CompositeDisposable disposable) =>
            {
                _schedulingCourseVM.MapToSchedulingCourses(Courses);
            });
        }
        

        private Task GenerateTimeTable()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            SchedulingCourseVM.Dispose();
        }
        
    }
}
