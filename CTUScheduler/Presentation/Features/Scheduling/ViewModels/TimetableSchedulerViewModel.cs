using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
using CTUScheduler.Presentation.Features.Pagination.ViewModels;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using CTUScheduler.Presentation.Shared.Models;
using DynamicData;
using DynamicData.Binding;
using Material.Styles.Themes.Base;
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
        private CancellationTokenSource? _cts;
        private bool _isGeneratingTimeTable = false;
        
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
        public TimetableSchedulerViewModel(SourceList<Course> courses)
        {
            _dialogHostService = App.ServiceProvider!.GetRequiredService<IDialogHostService>();
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
            
            courses.Connect()
                .Bind(out var courseBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            this.WhenActivated((CompositeDisposable disposable) =>
            {
                SchedulingCourseOptionVM.MapToSchedulingCourses(courseBindable);
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
            PaginationViewModel.Clear();
            await Task.Run(() =>
            {
                var batch = new List<SelectableTimetableLayout>();
                foreach (var tableData in Combinatorics.CartesianProduct(
                             sets,
                             prefix => _scheduleValidator.IsValidTimeTableFromRaw(prefix),
                             full => true,
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
