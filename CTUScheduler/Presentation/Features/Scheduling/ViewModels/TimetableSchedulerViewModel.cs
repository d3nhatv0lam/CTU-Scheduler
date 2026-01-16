using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleService.Interfaces;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Validators;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.TimetableDialog;
using CTUScheduler.Presentation.Shared.Mappers;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Academic;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels;

public class TimetableSchedulerViewModel : ViewModelBase, IStepViewModel, IDisposable, IActivatableViewModel,
    INextStepCondition, ICleanupAsync
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IScheduleRegistrationService _scheduleRegistrationService;
    private readonly ITimetableDialogService _timetableDialogService;
    private readonly SchedulingCourseOptionViewModel _schedulingCourseOptionVM;
    private readonly SelectableTimeTablesPaginationUi _paginationTimeTableViewModel;
    private readonly CourseMapper _courseMapper = new();
    private readonly ObservableAsPropertyHelper<string> _limitTimetableSelectedDisplayedHelper;
    private readonly ObservableAsPropertyHelper<bool> _isNextStepEnabled;
    private CancellationTokenSource? _cts;
    private bool _isGeneratingTimeTable;


    public bool IsGeneratingTimeTable
    {
        get => _isGeneratingTimeTable;
        set => this.RaiseAndSetIfChanged(ref _isGeneratingTimeTable, value);
    }

    public ViewModelActivator Activator { get; } = new ();
    public SchedulingCourseOptionViewModel SchedulingCourseOptionVM => _schedulingCourseOptionVM;
    public SelectableTimeTablesPaginationUi PaginationTimeTableViewModel => _paginationTimeTableViewModel;
    public string LimitTimetableSelectedDisplayed => _limitTimetableSelectedDisplayedHelper.Value;

    public bool IsNextStepEnabled => _isNextStepEnabled.Value;
    public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SourceList<CourseUi> courses)
    {
        _timetableDialogService = App.ServiceProvider.GetRequiredService<ITimetableDialogService>();
        _schedulingCourseOptionVM = new SchedulingCourseOptionViewModel();
        _scheduleRegistrationService = App.ServiceProvider.GetRequiredService<IScheduleRegistrationService>();

        var profileQueryService = App.ServiceProvider.GetRequiredService<IProfileQueryService>();
        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();
        _paginationTimeTableViewModel = new(12, maxCanSelect);

        _limitTimetableSelectedDisplayedHelper = this.WhenAnyValue(
                x => x.PaginationTimeTableViewModel.SelectedTimetableCount,
                x => x.PaginationTimeTableViewModel.MaxPageCanSelect)
            .Select(tuple =>
            {
                var (selectedPageCount, maxPageCanSelect) = tuple;
                return $"{selectedPageCount}/{maxPageCanSelect}";
            })
            .ToProperty(this, nameof(LimitTimetableSelectedDisplayed))
            .DisposeWith(_disposables);

        _isNextStepEnabled = PaginationTimeTableViewModel.SelectedTimetableCountChanged
            .Select(count => count > 0)
            .ToProperty(this, nameof(IsNextStepEnabled), scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(_disposables);

        GenerateTimeTableCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsGeneratingTimeTable)
                {
                    StopGenerateTimeTable();
                    return;
                }
                IsGeneratingTimeTable = true;
                var courseSectionFlatten = CourseSectionsTrackerFlatten(SchedulingCourseOptionVM.GetGroupedCourses());
                await GenerateTimeTable(courseSectionFlatten);
                IsGeneratingTimeTable = false;
            })
            .DisposeWith(_disposables);

        ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<SelectableTimetableLayout>(async selectableTimetableLayout =>
               await _timetableDialogService.ShowTimetableDetails(selectableTimetableLayout.Item))
            .DisposeWith(_disposables);

        this.WhenActivated(disposable =>
        {
            courses.Connect()
                .Transform(courseUi => _courseMapper.ToCourse(courseUi))
                .Bind(out var courseBindable)
                .Subscribe(_ => SchedulingCourseOptionVM.MapToSchedulingCourses(courseBindable))
                .DisposeWith(disposable);
        });
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
        PaginationTimeTableViewModel.Clear();
        await Task.Run(() =>
        {
            var batch = new List<SelectableTimetableLayout>();
            foreach (var tableData in Combinatorics.CartesianProduct(
                         sets,
                     (currentPath,next) => ScheduleValidator.ValidateStep(currentPath,next),
                         _ => true,
                         _cts.Token))
            {
                var layout = new TimetablePreviewViewModel(tableData);
                
                var selectableLayoutViewModel = new SelectableTimetableLayout(layout);
                batch.Add(selectableLayoutViewModel);

                if (batch.Count > 30)
                {
                    var copyList = batch.ToList();
                    batch.Clear();

                    RxApp.MainThreadScheduler.Schedule(() => PaginationTimeTableViewModel.AddAll(copyList));
                }
            }

            if (batch.Count > 0)
            {
                var copyList = batch.ToList();
                batch.Clear();
                RxApp.MainThreadScheduler.Schedule(() => PaginationTimeTableViewModel.AddAll(copyList));
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

    public async Task CleanupAsync()
    {
        var blueprints = (await PaginationTimeTableViewModel.GetSelectedTimetables())
            .Select(x => x.Item.ToScheduleBlueprint());
        _scheduleRegistrationService.RegisterBlueprint(blueprints);
        PaginationTimeTableViewModel.Clear();
    }

    public void Dispose()
    {
        SchedulingCourseOptionVM.Dispose();
        PaginationTimeTableViewModel.Dispose();
        _disposables.Dispose();
    }
}