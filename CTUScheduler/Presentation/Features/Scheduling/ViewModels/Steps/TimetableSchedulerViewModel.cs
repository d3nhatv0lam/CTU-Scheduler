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
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Pagination.Models;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using CTUScheduler.Presentation.Services.UserInteractionService.Models.Dialogs;
using CTUScheduler.Presentation.Services.Factories;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using DynamicData;
using DynamicData.Binding;
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
    private readonly ITimetableGeneratorService _timetableGeneratorService;

    [Reactive] private SchedulingPreset? _selectedPreset;

    public IReadOnlyList<SchedulingPreset> Presets { get; } = SchedulingPresetViewModel.DefaultPresets;
    public SchedulingCourseCoordinatorViewModel SchedulingCourseCoordinatorVM { get; }
    public TimetablePaginationViewModel PaginationTimeTableViewModel { get; }


    public IObservable<bool> CanNavigateNext { get; }
    public ReactiveCommand<Unit, IReadOnlyList<RawTimetableData>> GenerateTimeTableCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelGenerationCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SchedulingWizardContext context,
        IScheduleRegistrationService scheduleRegistrationService,
        ITimetableGeneratorService timetableGeneratorService,
        IUserInteractionService userInteractionService,
        IProfileQueryService profileQueryService,
        IViewModelFactory viewModelFactory,
        ILoggerFactory loggerFactory)
    {
        _scheduleRegistrationService = scheduleRegistrationService;
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

        _selectedPreset = Presets[0];

        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();


        PaginationTimeTableViewModel = new TimetablePaginationViewModel(
                maxCanSelect,
                new PaginationOptions<SelectableTimetableLayout>
                {
                    PageSize = 12,
                    SortObservable =
                        Observable.Return(
                            SortExpressionComparer<SelectableTimetableLayout>.Descending(x => x.Item.TotalScore)),
                    AutoRefresh =
                    [
                        new AutoRefreshOptions<SelectableTimetableLayout>
                        {
                            Property = x => x.Item.TotalScore,
                            RefreshBuffer = TimeSpan.FromMilliseconds(300)
                        }
                    ]
                })
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.SelectedPreset)
            .WhereNotNull()
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Subscribe(preset =>
            {
                var scorers = preset.Profile.Scorers;
                foreach (var layout in PaginationTimeTableViewModel.CurrentData)
                {
                    var vm = layout.Item;
                    vm.TotalScore = timetableGeneratorService.RecalculateScore(vm.Choices, scorers);
                }
            }).DisposeWith(_disposables);

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
                        var scorers = SelectedPreset?.Profile.Scorers;

                        var options = new ScheduleGenerationOptions()
                        {
                            CancellationToken = cts.Token,
                            MaxResults = 1000,
                            Scorers = scorers ?? [],
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
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(rawTimetableData =>
            {
                var timetableLayout =
                    rawTimetableData.Select(x =>
                    {
                        var layout =
                            viewModelFactory.Create<TimetablePreviewViewModel, IReadOnlyList<SectionChoice>>(x.Choices);
                        layout.TotalScore = x.Score;

                        return new SelectableTimetableLayout(layout);
                    });

                PaginationTimeTableViewModel.AddRange(timetableLayout);
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