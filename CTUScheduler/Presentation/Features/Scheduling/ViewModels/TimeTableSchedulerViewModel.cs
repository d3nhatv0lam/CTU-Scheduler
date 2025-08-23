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
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.AppServices.Services.User;
using CTUScheduler.AppServices.Validators;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.TimeTable.Models;
using CTUScheduler.Presentation.Features.TimeTable.ViewModels;
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
        private readonly IDialogHostService _dialogHostService;
        private readonly IUserDataService _userDataService;
        private readonly SchedulingCourseViewModel _schedulingCourseVM;
        private readonly ScheduleValidator _scheduleValidator = new ScheduleValidator();
        private readonly SourceList<Course> _coursesSourceList;
        private ObservableCollection<SelectableTimetableLayout> _timeTableLayoutViewModels = new ();
        private ReadOnlyObservableCollection<Course> _courseBindable;
        private CancellationTokenSource? _cts;
        private bool _isGeneratingTimeTable = false;
        
        public bool IsGeneratingTimeTable
        {
            get => _isGeneratingTimeTable;
            set => this.RaiseAndSetIfChanged(ref _isGeneratingTimeTable, value);
        }
        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public ReadOnlyObservableCollection<Course> Courses => _courseBindable;
        public SchedulingCourseViewModel SchedulingCourseVM => _schedulingCourseVM;
        public ObservableCollection<SelectableTimetableLayout> TimeTableLayoutViewModels => _timeTableLayoutViewModels;
        public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get;  }
        public ReactiveCommand<SelectableTimetableLayout,Unit> OpenTimetableDetailsCommand { get; }
        public TimeTableSchedulerViewModel(SourceList<Course> courses)
        {
            _coursesSourceList = courses;
            _userDataService = App.ServiceProvider!.GetRequiredService<IUserDataService>();
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
            _schedulingCourseVM = new SchedulingCourseViewModel();

            GenerateTimeTableCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    // if (IsGeneratingTimeTable)
                    // {
                    //     StopGenerateTimeTable();
                    //     return;
                    // }
                    //
                    // IsGeneratingTimeTable = true;
                    var courseSectionFlatten = CourseSectionsTrackerFlatten(SchedulingCourseVM.GetGroupedCourses());
                    await GenerateTimeTable(courseSectionFlatten);
                })
                .DisposeWith(_disposables);
            
            OpenTimetableDetailsCommand = ReactiveCommand
                .Create<SelectableTimetableLayout>((selectableTimetableLayout) =>
                    OpenTimetableDetails(selectableTimetableLayout))
                .DisposeWith(_disposables);
            
            TimeTableLayoutViewModels.ToObservableChangeSet()
                .DisposeMany()
                .Subscribe()
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

        private void OpenTimetableDetails(SelectableTimetableLayout selectableTimetableLayout)
        {
            var timetableLayoutViewModel = selectableTimetableLayout.Item;
            _dialogHostService.ShowDialogAsync<Unit>(timetableLayoutViewModel,
                DialogHostService.DialogIdentifier.Timetable);
        }

        private void StopGenerateTimeTable()
        {
            _cts?.Cancel();
            IsGeneratingTimeTable = false;
        }
        
        
        private async Task GenerateTimeTable(IEnumerable<List<SectionChoice>> sets)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            TimeTableLayoutViewModels.Clear();
            foreach (var tableData in Combinatorics.CartesianProduct(
                         sets,
                         prefix => _scheduleValidator.IsValidTimeTableFromRaw(prefix),
                         full => true,
                         _cts.Token))
            {
                var layout = new TimeTableLayoutViewModel(new ScheduleTable());
                foreach (var data in tableData)
                {
                    layout.AddCourseSectionToTable(data);
                }
                var selectableLayoutViewModel = new SelectableTimetableLayout(layout);
                TimeTableLayoutViewModels.Add(selectableLayoutViewModel);
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
