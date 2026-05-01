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
using CTUScheduler.Presentation.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Models;
using CTUScheduler.Presentation.Shared.Models.Identifiers;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;


namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;

public class TimetableSchedulerViewModel : ViewModelBase, IWizardStep, IDisposable, IActivatableViewModel,
    IFinishableStep,
    INeedArgs<SchedulingWizardContext>
{
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly IScheduleRegistrationService _scheduleRegistrationService;
    private readonly SchedulingCourseOptionViewModel _schedulingCourseOptionVM;
    private readonly TimetablePaginationViewModel _paginationTimeTableViewModel;
    private readonly IUserInteractionService _userInteractionService;
    private readonly ObservableAsPropertyHelper<string> _limitTimetableSelectedDisplayedHelper;


    private CancellationTokenSource? _cts;
    private bool _isGeneratingTimeTable;

    public bool IsGeneratingTimeTable
    {
        get => _isGeneratingTimeTable;
        set => this.RaiseAndSetIfChanged(ref _isGeneratingTimeTable, value);
    }

    public ViewModelActivator Activator { get; } = new();
    public SchedulingCourseOptionViewModel SchedulingCourseOptionVM => _schedulingCourseOptionVM;
    public TimetablePaginationViewModel PaginationTimeTableViewModel => _paginationTimeTableViewModel;
    public string LimitTimetableSelectedDisplayed => _limitTimetableSelectedDisplayedHelper.Value;
    public IObservable<bool> CanNavigateNext { get; }
    public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; }
    public ReactiveCommand<SelectableTimetableLayout, Unit> ShowTimetableDetailsCommand { get; }

    public TimetableSchedulerViewModel(SchedulingWizardContext context)
    {
        _schedulingCourseOptionVM = new SchedulingCourseOptionViewModel();
        _scheduleRegistrationService = App.ServiceProvider.GetRequiredService<IScheduleRegistrationService>();
        _userInteractionService = App.ServiceProvider.GetRequiredService<IUserInteractionService>();

        var profileQueryService = App.ServiceProvider.GetRequiredService<IProfileQueryService>();
        var maxCanSelect = profileQueryService.ProfileUsageState
            .Select(x => x.Limit - x.Current)
            .DistinctUntilChanged();
        _paginationTimeTableViewModel = new(maxCanSelect);

        _limitTimetableSelectedDisplayedHelper =
            this.WhenAnyValue(
                    x => x.PaginationTimeTableViewModel.SelectedItemCount,
                    x => x.PaginationTimeTableViewModel.MaxItemCanSelect)
                .Select(tuple =>
                {
                    var (selectedPageCount, maxPageCanSelect) = tuple;
                    return $"{selectedPageCount}/{maxPageCanSelect}";
                })
                .ToProperty(this, nameof(LimitTimetableSelectedDisplayed))
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
                var courseSectionFlatten = CourseSectionsTrackerFlatten(SchedulingCourseOptionVM.GetGroupedCourses());
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
                await _userInteractionService.Dialog.ShowModal<TimetableLayoutBaseViewModel, Unit>(
                    selectableTimetableLayout.Item, options);
            })
            .DisposeWith(_disposables);

        this.WhenActivated(disposable =>
        {
            context.SelectedCourses.Connect()
                .Transform(node => node.CoreCourse.WithSections(node.Sections))
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

        // Lấy Excel exporter từ DI một lần, rồi truyền vào mỗi TimetablePreviewViewModel
        var excelExporter = App.ServiceProvider.GetRequiredService<IExcelExporterService>();

        await Task.Run(() =>
        {
            var batch = new List<SelectableTimetableLayout>();
            foreach (var tableData in Combinatorics.CartesianProduct(
                         sets,
                         (currentPath, next) => ScheduleValidator.ValidateStep(currentPath, next),
                         _ => true,
                         _cts.Token))
            {
                var layout = new TimetablePreviewViewModel(tableData, excelExporter);

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

    private IEnumerable<List<SectionChoice>> CourseSectionsTrackerFlatten(IEnumerable<List<Course>> courseSets)
    {
        return courseSets
            .Select(group => group
                .SelectMany(course => course.Sections.Select(section => new SectionChoice(course, section)))
                .ToList()
            );
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
        SchedulingCourseOptionVM.Dispose();
        PaginationTimeTableViewModel.Dispose();
        _disposables.Dispose();
    }
}