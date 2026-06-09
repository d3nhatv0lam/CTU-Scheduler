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
using CTUScheduler.Core.Models.Scoring;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Pagination.Models;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;


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
    private readonly IControlRendererService _controlRendererService;
    private readonly ITimetablePreviewRenderer _timetablePreviewRenderer;
    private readonly IUserInteractionService _userInteractionService;

    public IReadOnlyList<SchedulingPreset> Presets { get; } = SchedulingPresetViewModel.DefaultPresets;
    
    [Reactive] private SchedulingPreset? _selectedPreset;

    public SchedulingCourseCoordinatorViewModel SchedulingCourseCoordinatorVM { get; }
    public TimetablePaginationViewModel PaginationTimeTableViewModel { get; }

    private double CalculateScore(IReadOnlyList<SectionChoice> choices, IReadOnlyList<IScheduleScorer> scorers)
    {
        if (scorers == null || scorers.Count == 0) return 0.0;
        double totalWeight = 0.0;
        double totalScore = 0.0;
        foreach (var scorer in scorers)
        {
            totalScore += scorer.CalculateScore(choices) * scorer.Weight;
            totalWeight += scorer.Weight;
        }
        return totalWeight > 0 ? totalScore / totalWeight : 0.0;
    }
    public IObservable<bool> CanNavigateNext { get; }
    public ReactiveCommand<Unit, SelectableTimetableLayout> GenerateTimeTableCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelGenerationCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SchedulingWizardContext context,
        IScheduleRegistrationService scheduleRegistrationService,
        ITimetableGeneratorService timetableGeneratorService,
        IUserInteractionService userInteractionService,
        IProfileQueryService profileQueryService,
        IExcelExporterService excelExporterService,
        IControlRendererService controlRendererService,
        ITimetablePreviewRenderer timetablePreviewRenderer,
        ILoggerFactory loggerFactory)
    {
        _scheduleRegistrationService = scheduleRegistrationService;
        _excelExporter = excelExporterService;
        _timetableGeneratorService = timetableGeneratorService;
        _controlRendererService = controlRendererService;
        _timetablePreviewRenderer = timetablePreviewRenderer;
        _userInteractionService = userInteractionService;
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

        _selectedPreset = Presets.FirstOrDefault();

        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();

        var sortObservable = this.WhenAnyValue(x => x.SelectedPreset)
            .Select(preset =>
            {
                if (preset != null && PaginationTimeTableViewModel != null)
                {
                    var scorers = preset.Profile.Scorers;
                    foreach (var layout in PaginationTimeTableViewModel.CurrentData)
                    {
                        var vm = layout.Item;
                        vm.TotalScore = CalculateScore(vm.Choices, scorers);
                    }
                }
                return Comparer<SelectableTimetableLayout>.Create((a, b) =>
                    b.Item.TotalScore.CompareTo(a.Item.TotalScore));
            });

        PaginationTimeTableViewModel = new TimetablePaginationViewModel(
            maxCanSelect,
            new PaginationOptions<SelectableTimetableLayout>
            {
                SortObservable = sortObservable,
                PageSize = 12
            })
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
                            MaxResults = null
                        };

                        var scorers = SelectedPreset?.Profile.Scorers;

                        return _timetableGeneratorService.Generate(courseSectionFlatten, options)
                            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
                            .Select(x =>
                            {
                                var vm = new TimetablePreviewViewModel(x, _excelExporter, _controlRendererService, _timetablePreviewRenderer, _userInteractionService);
                                if (scorers != null)
                                {
                                    vm.TotalScore = CalculateScore(vm.Choices, scorers);
                                }
                                return new SelectableTimetableLayout(vm);
                            })
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
            .Buffer(TimeSpan.FromMilliseconds(500), PaginationTimeTableViewModel.PageSize * 2)
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