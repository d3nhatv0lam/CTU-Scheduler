using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.AppServices.Validators;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class TimeTableSchedulerViewModel: ViewModelBase, IStepViewModel, IDisposable , IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IUserDataService _userDataService;
        private readonly SchedulingCourseViewModel _schedulingCourseVM;
        private readonly ScheduleValidator _scheduleValidator = new ScheduleValidator();
        private readonly SourceList<Course> _coursesSourceList;
        private ReadOnlyObservableCollection<Course> _courseBindable;
        private CancellationTokenSource? _cts;
        private bool _isGeneratingTimeTable = false;
        
        /// <summary>
        /// Course Section tracker
        /// </summary>
        /// <param name="Course"></param>
        /// <param name="Section"></param>
        public record SectionChoice(Course Course, CourseData Section);


        public bool IsGeneratingTimeTable
        {
            get => _isGeneratingTimeTable;
            set => this.RaiseAndSetIfChanged(ref _isGeneratingTimeTable, value);
        }

        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public ReadOnlyObservableCollection<Course> Courses => _courseBindable;
        public SchedulingCourseViewModel SchedulingCourseVM => _schedulingCourseVM;
        public ObservableCollection<ScheduleTable> ScheduleTables { get; set; } = new ObservableCollection<ScheduleTable>();
        
        public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; protected set; }

        public TimeTableSchedulerViewModel(SourceList<Course> courses)
        {
            _coursesSourceList = courses;
            _userDataService = App.ServiceProvider!.GetRequiredService<IUserDataService>();
            _schedulingCourseVM = new SchedulingCourseViewModel();

            GenerateTimeTableCommand = ReactiveCommand.Create( () =>
                {
                    // if (IsGeneratingTimeTable)
                    // {
                    //     StopGenerateTimeTable();
                    //     return;
                    // }
                    //
                    // IsGeneratingTimeTable = true;
                    var courseSectionFlatten = CourseSectionsTrackerFlatten(SchedulingCourseVM.GetGroupedCourses());
                    GenerateTimeTable(courseSectionFlatten);
                })
                .DisposeWith(_disposables);
            
            _coursesSourceList.Connect()
                .Bind(out _courseBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            this.WhenActivated((CompositeDisposable disposable) =>
            {
                SchedulingCourseVM.MapToSchedulingCourses(Courses);
            });
        }
        

        private void StopGenerateTimeTable()
        {
            _cts?.Cancel();
            IsGeneratingTimeTable = false;
        }
        
        
        private void GenerateTimeTable(IEnumerable<List<SectionChoice>> sets)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            foreach (var tableData in Combinatorics.CartesianProduct(
                         sets,
                         prefix => _scheduleValidator.IsValidTimeTableFromRaw(prefix),
                         full => true,
                         _cts.Token))
            {
                foreach (var table in tableData)
                {
                    Debug.WriteLine(table.Course.Code + " " + table.Section.Group);;
                }
                Debug.WriteLine("");
            }
        }
        
        private IEnumerable<List<SectionChoice>> CourseSectionsTrackerFlatten(IEnumerable<List<Course>> courseSets)
        {
            return courseSets
                .Select(group => group
                    .SelectMany(course => course.Sections.Select(section => new SectionChoice(course, section)))
                    .ToList()
                );
        }

        public void Dispose()
        {
            SchedulingCourseVM.Dispose();
            _disposables.Dispose();
        }
    }
}
