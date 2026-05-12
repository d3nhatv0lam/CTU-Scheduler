using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps;
using CTUScheduler.Presentation.Services.Factories;
using DynamicData;

namespace CTUScheduler.Presentation.Features.Scheduling.Models.Strategies;

public class QuickSchedulingStrategy : SchedulingStrategy
{
    private readonly ICourseCatalogService _courseCatalogService;
    private readonly IReadOnlyList<string> _plannedCoursesCode;
    private const string ExceptCode = "SHCVHT";
    public override string Name => nameof(QuickSchedulingStrategy);
    public override int StartStepIndex => 1;

    public QuickSchedulingStrategy(IViewModelFactory factory,
        ICourseCatalogService courseCatalogService,
        IPlannedCourseStore plannedCourseStore) : base(factory)
    {
        _courseCatalogService = courseCatalogService;

        var plannedCourses = plannedCourseStore.CurrentPlannedCourses ?? [];

        _plannedCoursesCode = plannedCourses
            .Select(x => x.Code)
            .Where(x => !string.Equals(x, ExceptCode))
            .ToList();
    }

    public override async Task<IWizardStep[]> CreateStepsAsync(SchedulingWizardContext context,
        CompositeDisposable disposables)
    {
        await InjectPlannedCourses(context);

        var step1 = Factory.Create<FindCourseViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);
        var step2 = Factory.Create<TimetableSchedulerViewModel, SchedulingWizardContext>(context)
            .DisposeWith(disposables);

        return [step1, step2];
    }

    private async Task InjectPlannedCourses(SchedulingWizardContext context)
    {
        var coursesBlueprints = new List<CourseBlueprint>();
        await foreach (var course in _courseCatalogService.FetchCoursesBatchAsync(_plannedCoursesCode))
        {
            var courseBlueprint = new CourseBlueprint(course, course.Sections);
            coursesBlueprints.Add(courseBlueprint);
            // context.CourseBlueprints.Add(courseBlueprint);
        }
        context.CourseBlueprints.AddRange(coursesBlueprints);
    }
}