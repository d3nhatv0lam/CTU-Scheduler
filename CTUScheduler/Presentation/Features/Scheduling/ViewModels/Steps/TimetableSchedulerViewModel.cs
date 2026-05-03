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
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Validators;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;


namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;

public partial class TimetableSchedulerViewModel : ViewModelBase, IWizardStep, IDisposable, IActivatableViewModel,
    IFinishableStep,
    INeedArgs<SchedulingWizardContext>
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<TimetableSchedulerViewModel> _logger;
    private readonly IScheduleRegistrationService _scheduleRegistrationService;
    private readonly IExcelExporterService _excelExporter;

    private CancellationTokenSource? _cts;
    [Reactive] private bool _isGeneratingTimeTable;

    public ViewModelActivator Activator { get; } = new();
    public SchedulingCourseCoordinatorViewModel SchedulingCourseCoordinatorVM { get; }
    public TimetablePaginationViewModel PaginationTimeTableViewModel { get; }
    public IObservable<bool> CanNavigateNext { get; }
    public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SchedulingWizardContext context,
        IScheduleRegistrationService scheduleRegistrationService,
        IUserInteractionService userInteractionService,
        IProfileQueryService profileQueryService,
        IExcelExporterService excelExporterService,
        ILoggerFactory loggerFactory)
    {
        _scheduleRegistrationService = scheduleRegistrationService;
        _excelExporter = excelExporterService;
        _logger = loggerFactory.CreateLogger<TimetableSchedulerViewModel>();
        
        var courseStream = context.CourseBlueprints.Connect()
            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
            .AutoRefreshOnObservable(x => x.SectionsSourceChanges.Skip(1))
            .Transform(node => node.CoreCourse.WithSections(node.Sections), transformOnRefresh:true)
            .Transform(course => new SchedulingCourseViewModel(course, loggerFactory.CreateLogger<SchedulingCourseViewModel>()))
            .DisposeMany()
            .AsObservableList()
            .DisposeWith(_disposables);
        SchedulingCourseCoordinatorVM = new SchedulingCourseCoordinatorViewModel(courseStream, loggerFactory.CreateLogger<SchedulingCourseCoordinatorViewModel>())
            .DisposeWith(_disposables);

        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();
        PaginationTimeTableViewModel = new TimetablePaginationViewModel(maxCanSelect)
            .DisposeWith(_disposables);

        CanNavigateNext = PaginationTimeTableViewModel.SelectedItemCountChanged
            .Select(count => count > 0);

        GenerateTimeTableCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (IsGeneratingTimeTable)
                {
                    StopGenerateTimeTable();
                    return;
                }

                IsGeneratingTimeTable = true;
                var courseSectionFlatten =
                    CourseSectionsTrackerFlatten(SchedulingCourseCoordinatorVM.GetGroupedCourses());
                await GenerateTimeTable(courseSectionFlatten);
                IsGeneratingTimeTable = false;
            })
            .DisposeWith(_disposables);

        ShowTimetableDetailsCommand = ReactiveCommand.CreateFromTask<SelectableTimetableLayout>(async
                selectableTimetableLayout =>
            {
                var options = new DialogOptions
                {
                    SizeMode = DialogSizeMode.Responsive,
                    IsCloseButtonVisible = true,
                    CanLightDismiss = true,
                    HostId = DialogIds.Root
                };
                await userInteractionService.Dialog.ShowModal<TimetableLayoutBaseViewModel, Unit>(
                    selectableTimetableLayout.Item, options);
            })
            .DisposeWith(_disposables);
    }


    private void StopGenerateTimeTable()
    {
        _cts?.Cancel();
        IsGeneratingTimeTable = false;
    }

    private async Task GenerateTimeTable(IReadOnlyList<IReadOnlyList<SectionChoice>> sets)
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
                         (currentPath, next) => ScheduleValidator.ValidateStep(currentPath, next),
                         _ => true,
                         _cts.Token))
            {
                var layout = new TimetablePreviewViewModel(tableData, _excelExporter);

                var selectableLayoutViewModel = new SelectableTimetableLayout(layout);
                batch.Add(selectableLayoutViewModel);

                if (batch.Count > 30)
                {
                    var copyList = batch.ToList();
                    batch.Clear();

                    RxApp.MainThreadScheduler.Schedule(() => PaginationTimeTableViewModel.AddRange(copyList));
                }
            }

            if (batch.Count > 0)
            {
                var copyList = batch.ToList();
                batch.Clear();
                RxApp.MainThreadScheduler.Schedule(() => PaginationTimeTableViewModel.AddRange(copyList));
            }
        });
    }

    private IReadOnlyList<IReadOnlyList<SectionChoice>> CourseSectionsTrackerFlatten(
        IReadOnlyList<IReadOnlyList<Course>> courseSets)
    {
        return courseSets
            .Select(group => group
                .SelectMany(course => course.Sections.Select(section => new SectionChoice(course, section)))
                .ToList()
            ).ToList();
    }

    public void Commit()
    {
        var blueprints = PaginationTimeTableViewModel.GetSelectedItems()
            .Select(x => x.Item.ToScheduleBlueprint());
        _scheduleRegistrationService.RegisterBlueprint(blueprints);
        PaginationTimeTableViewModel.Clear();
    }


    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("{this}: Disposed", nameof(TimetableSchedulerViewModel));
    }
}