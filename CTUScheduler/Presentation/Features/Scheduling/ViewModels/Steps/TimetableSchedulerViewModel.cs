using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
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
using Microsoft.Extensions.Logging;
using ReactiveUI;


namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;

public partial class TimetableSchedulerViewModel : ViewModelBase, IWizardStep, IDisposable,
    IFinishableStep,
    INeedArgs<SchedulingWizardContext>
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<TimetableSchedulerViewModel> _logger;
    private readonly IScheduleRegistrationService _scheduleRegistrationService;
    private readonly IExcelExporterService _excelExporter;
    private readonly ITimetableGeneratorService _timetableGeneratorService;

    public SchedulingCourseCoordinatorViewModel SchedulingCourseCoordinatorVM { get; }
    public TimetablePaginationViewModel PaginationTimeTableViewModel { get; }
    public IObservable<bool> CanNavigateNext { get; }
    public ReactiveCommand<Unit, IReadOnlyList<SectionChoice>> GenerateTimeTableCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelGenerationCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SchedulingWizardContext context,
        IScheduleRegistrationService scheduleRegistrationService,
        ITimetableGeneratorService timetableGeneratorService,
        IUserInteractionService userInteractionService,
        IProfileQueryService profileQueryService,
        IExcelExporterService excelExporterService,
        ILoggerFactory loggerFactory)
    {
        _scheduleRegistrationService = scheduleRegistrationService;
        _excelExporter = excelExporterService;
        _timetableGeneratorService = timetableGeneratorService;
        _logger = loggerFactory.CreateLogger<TimetableSchedulerViewModel>();

        var courseStream = context.CourseBlueprints.Connect()
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .AutoRefreshOnObservable(x => x.SectionsSourceChanges.Skip(1))
            .Transform(node => node.CoreCourse.WithSections(node.Sections), transformOnRefresh: true)
            .Transform(course =>
                new SchedulingCourseViewModel(course, loggerFactory.CreateLogger<SchedulingCourseViewModel>()))
            .DisposeMany()
            .AsObservableList()
            .DisposeWith(_disposables);

        SchedulingCourseCoordinatorVM = new SchedulingCourseCoordinatorViewModel(courseStream,
                loggerFactory.CreateLogger<SchedulingCourseCoordinatorViewModel>())
            .DisposeWith(_disposables);

        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();

        PaginationTimeTableViewModel = new TimetablePaginationViewModel(maxCanSelect)
            .DisposeWith(_disposables);

        CanNavigateNext = PaginationTimeTableViewModel.SelectedItemCountChanged
            .Select(count => count > 0);

        CancelGenerationCommand = ReactiveCommand.Create(
                () => _logger.LogInformation("Timetable generation cancelled by user."),
                this.WhenAnyObservable(x => x.GenerateTimeTableCommand.IsExecuting))
            .DisposeWith(_disposables);

        GenerateTimeTableCommand = ReactiveCommand.CreateFromObservable(() =>
                Observable.Using(
                    () => new CancellationTokenSource(),
                    cts =>
                    {
                        PaginationTimeTableViewModel.Clear();
                        var courseSectionFlatten =
                            CourseSectionsTrackerFlatten(SchedulingCourseCoordinatorVM.GetGroupedCourses());

                        var options = new ScheduleGenerationOptions()
                        {
                            CancellationToken = cts.Token,
                        };
                        return _timetableGeneratorService.Generate(courseSectionFlatten, options)
                            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
                            .TakeUntil(CancelGenerationCommand.Do(_ =>
                            {
                                try
                                {
                                    cts.Cancel();
                                }
                                catch (ObjectDisposedException)
                                {
                                }
                            }));
                    }))
            .DisposeWith(_disposables);

        GenerateTimeTableCommand
            .Select(x => new SelectableTimetableLayout(new TimetablePreviewViewModel(x, _excelExporter)))
            .Buffer(TimeSpan.FromMilliseconds(100), PaginationTimeTableViewModel.PageSize)
            .Where(batch => batch.Count > 0)
            .Subscribe(batch => { PaginationTimeTableViewModel.AddRange(batch); })
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
        _logger.LogDebug("Disposed");
    }
}