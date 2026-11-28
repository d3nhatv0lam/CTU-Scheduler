using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Grades.ViewModels.Components;

public sealed record SemesterFilterOption(
    int? AcademicYearStart,
    int? Term,
    string DisplayName,
    string SelectedDisplayName,
    IReadOnlyList<SemesterFilterOption> Children)
{
    public static readonly SemesterFilterOption All = new(
        null,
        null,
        "Tất cả học kỳ",
        "Tất cả học kỳ",
        []);

    public override string ToString() => SelectedDisplayName;
}

public sealed partial class GradeTranscriptListViewModel : ViewModelBase, IDisposable
{
    private sealed record CachedSemesterGrades(
        SemesterGrade Semester,
        IReadOnlyList<CourseGradeItemViewModel> Items);

    private readonly CompositeDisposable _disposables = new();
    private readonly BehaviorSubject<IReadOnlyList<CachedSemesterGrades>> _cachedSemesters = new([]);
    private bool _isDisposed;

    [Reactive] private string? _searchText = string.Empty;
    [Reactive] private SemesterFilterOption? _selectedSemester = SemesterFilterOption.All;
    [Reactive] private bool _hideAdvisoryCourses = true;
    [Reactive] private IReadOnlyList<SemesterFilterOption> _semesterOptions = [SemesterFilterOption.All];

    [ObservableAsProperty] private IReadOnlyList<SemesterGradeGroupViewModel> _semesterGroups = [];
    [ObservableAsProperty] private bool _isEmptyFilterResult;

    public GradeTranscriptListViewModel()
    {
        _cachedSemesters.DisposeWith(_disposables);

        var debouncedSearch = this.WhenAnyValue(x => x.SearchText)
            .Select(s => (s ?? string.Empty).Trim())
            .Throttle(TimeSpan.FromMilliseconds(300), RxSchedulers.TaskpoolScheduler)
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        var instantSearchReset = this.WhenAnyValue(x => x.SearchText)
            .Where(string.IsNullOrWhiteSpace)
            .Select(_ => string.Empty);

        var searchQuery = Observable.Merge(debouncedSearch, instantSearchReset)
            .DistinctUntilChanged();

        var groupsStream = Observable.CombineLatest(
                _cachedSemesters,
                searchQuery,
                this.WhenAnyValue(x => x.SelectedSemester),
                this.WhenAnyValue(x => x.HideAdvisoryCourses),
                BuildGroups)
            .Publish()
            .RefCount();

        _semesterGroupsHelper = groupsStream
            .ToProperty(this, nameof(SemesterGroups), initialValue: [])
            .DisposeWith(_disposables);

        _isEmptyFilterResultHelper = groupsStream
            .CombineLatest(
                _cachedSemesters,
                (groups, cached) => cached.Count > 0 && groups.Count == 0)
            .ToProperty(this, nameof(IsEmptyFilterResult), initialValue: false)
            .DisposeWith(_disposables);
    }

    public void UpdateTranscript(GradeTranscript? transcript)
    {
        var previousSelection = SelectedSemester;

        if (transcript is null || transcript.Semesters.Count == 0)
        {
            _cachedSemesters.OnNext([]);
            SemesterOptions = [SemesterFilterOption.All];
            SelectedSemester = SemesterFilterOption.All;
            return;
        }

        var orderedSemesters = transcript.Semesters
            .OrderByDescending(s => s.AcademicYearStart)
            .ThenByDescending(s => s.Term)
            .ToList();

        var cached = orderedSemesters
            .Select(s => new CachedSemesterGrades(
                s,
                [.. s.Courses.Select(c => new CourseGradeItemViewModel(c))]))
            .ToList();

        var yearNodes = orderedSemesters
            .GroupBy(s => s.AcademicYearStart)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                string yearLabel = $"Năm học {g.Key} - {g.Key + 1}";
                var children = g
                    .OrderByDescending(s => s.Term)
                    .Select(s => new SemesterFilterOption(
                        s.AcademicYearStart,
                        s.Term,
                        $"Học kỳ {s.Term}",
                        GradeVisualHelper.FormatSemesterTitle(s.AcademicYearStart, s.Term),
                        []))
                    .ToList();

                return new SemesterFilterOption(g.Key, null, yearLabel, yearLabel, children);
            })
            .ToList();

        var options = new List<SemesterFilterOption>(yearNodes.Count + 1)
        {
            SemesterFilterOption.All
        };
        options.AddRange(yearNodes);

        _cachedSemesters.OnNext(cached);
        SemesterOptions = options;

        var matchingSelection = previousSelection is not null
            ? options
                .SelectMany(o => o.Children.Count > 0 ? o.Children.Prepend(o) : [o])
                .FirstOrDefault(o =>
                    o.AcademicYearStart == previousSelection.AcademicYearStart &&
                    o.Term == previousSelection.Term)
            : null;
        SelectedSemester = matchingSelection ?? SemesterFilterOption.All;
    }

    private static IReadOnlyList<SemesterGradeGroupViewModel> BuildGroups(
        IReadOnlyList<CachedSemesterGrades> cachedSemesters,
        string keyword,
        SemesterFilterOption? activeSemester,
        bool hideAdvisoryCourses)
    {
        if (cachedSemesters.Count == 0)
        {
            return [];
        }

        bool hasSearch = !string.IsNullOrEmpty(keyword);
        bool hasYearFilter = activeSemester?.AcademicYearStart is not null;

        var groups = new List<SemesterGradeGroupViewModel>();
        int groupIndex = 0;

        foreach (var (semester, cachedItems) in cachedSemesters)
        {
            if (hasYearFilter)
            {
                if (semester.AcademicYearStart != activeSemester!.AcademicYearStart)
                {
                    continue;
                }

                if (activeSemester.Term.HasValue && semester.Term != activeSemester.Term.Value)
                {
                    continue;
                }
            }

            var filteredCourses = cachedItems
                .Where(c => !hideAdvisoryCourses || !c.IsAdvisoryActivity)
                .Where(c => !hasSearch ||
                            c.CourseCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            c.CourseName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filteredCourses.Count == 0)
            {
                continue;
            }

            bool expandedByDefault = hasSearch || hasYearFilter || groupIndex < 2;
            groups.Add(new SemesterGradeGroupViewModel(semester, filteredCourses, expandedByDefault));
            groupIndex++;
        }

        return groups;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _disposables.Dispose();
        _isDisposed = true;
    }
}
