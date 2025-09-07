using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.Dialogs;
using CTUScheduler.AppServices.Validators;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using CTUScheduler.Presentation.Shared.Mappers;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Academic;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class TimetableSchedulerViewModel: ViewModelBase, IStepViewModel, IDisposable , IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IDialogHostService _dialogHostService;
        private readonly SchedulingCourseOptionViewModel _schedulingCourseOptionVM;
        private readonly ScheduleValidator _scheduleValidator = new ScheduleValidator();
        private readonly PaginationViewModel<SelectableTimetableLayout> _paginationViewModel;
        private readonly CourseMapper _courseMapper = new();
        private CancellationTokenSource? _cts;
        private bool _isGeneratingTimeTable;
        
        public bool IsGeneratingTimeTable
        {
            get => _isGeneratingTimeTable;
            set => this.RaiseAndSetIfChanged(ref _isGeneratingTimeTable, value);
        }
        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public SchedulingCourseOptionViewModel SchedulingCourseOptionVM => _schedulingCourseOptionVM;
        public PaginationViewModel<SelectableTimetableLayout> PaginationViewModel => _paginationViewModel;
        public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; }
        public ReactiveCommand<SelectableTimetableLayout,Unit> OpenTimetableDetailsCommand { get; }
        public TimetableSchedulerViewModel(SourceList<CourseUi> courses)
        {
            _dialogHostService = App.ServiceProvider.GetRequiredService<IDialogHostService>();
            _schedulingCourseOptionVM = new SchedulingCourseOptionViewModel();
            _paginationViewModel = new(12);
            
            GenerateTimeTableCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    // if (IsGeneratingTimeTable)
                    // {
                    //     StopGenerateTimeTable();
                    //     return;
                    // }
                    //
                    // IsGeneratingTimeTable = true;
                    var courseSectionFlatten = CourseSectionsTrackerFlatten(SchedulingCourseOptionVM.GetGroupedCourses());
                    await GenerateTimeTable(courseSectionFlatten);
                })
                .DisposeWith(_disposables);
            
            OpenTimetableDetailsCommand = ReactiveCommand.Create<SelectableTimetableLayout>((selectableTimetableLayout) =>
                    OpenTimetableDetails(selectableTimetableLayout))
                .DisposeWith(_disposables);
 
            this.WhenActivated(disposable =>
            {
                // courses.Connect()
                //     .ObserveOn(RxApp.TaskpoolScheduler)
                //     .Transform(courseUi => _courseMapper.ToCourse(courseUi))
                //     .ObserveOn(RxApp.MainThreadScheduler)
                //     .Bind(out var courseBindable)
                //     .Subscribe()
                //     .DisposeWith(disposable);
                //
                // SchedulingCourseOptionVM.MapToSchedulingCourses(courseBindable);
                
                courses.Connect()
                    .Transform(courseUi => _courseMapper.ToCourse(courseUi))
                    .Bind(out var courseBindable)
                    .Subscribe(_ => SchedulingCourseOptionVM.MapToSchedulingCourses(courseBindable))
                    .DisposeWith(disposable);
            });
        }

        private void OpenTimetableDetails(SelectableTimetableLayout selectableTimetableLayout)
        {
            var timetableLayoutViewModel = selectableTimetableLayout.Item;
            _dialogHostService.ShowDialogAsync<Unit>(timetableLayoutViewModel, DialogHostService.DialogIdentifier.Timetable);
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
            PaginationViewModel.Clear();
            await Task.Run(() =>
            {
                var batch = new List<SelectableTimetableLayout>();
                foreach (var tableData in Combinatorics.CartesianProduct(
                             sets,
                             prefix => _scheduleValidator.IsValidTimeTableFromRaw(prefix),
                             _ => true,
                             _cts.Token))
                {
                    var layout = new TimetableLayoutViewModel(new ScheduleTable());
                    foreach (var data in tableData)
                        layout.AddCourseSectionToTable(data);
                    
                    var selectableLayoutViewModel = new SelectableTimetableLayout(layout);
                    batch.Add(selectableLayoutViewModel);

                    if (batch.Count > 30)
                    {
                        var copyList = batch.ToList();
                        batch.Clear();

                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            PaginationViewModel.AddAll(copyList);
                        });
                    }
                }
                if (batch.Count > 0)
                {
                    var copyList = batch.ToList();
                    batch.Clear();

                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        PaginationViewModel.AddAll(copyList);
                    });
                }
            });
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
            SchedulingCourseOptionVM.Dispose();
            _paginationViewModel.Dispose();
            _disposables.Dispose();
        }
    }
}
