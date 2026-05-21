using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps
{
    public partial class FindCourseViewModel : ViewModelBase, IDisposable, IWizardStep,
        INeedArgs<SchedulingWizardContext>
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly ILogger<FindCourseViewModel> _logger;
        [Reactive] private string _txtInputCourseKey = string.Empty;
        [Reactive] private bool _isOpenQuickSelectPopup;
        [Reactive] private bool _showOnlyAvailableSections;
        [Reactive] private bool _isTextBoxFocused;
        [Reactive] private QuickSelectDmhpCourse? _selectedQuickSelectDmhpCourse;

        [ObservableAsProperty] private Course _searchedCourse = null!;
        [ObservableAsProperty] private IReadOnlyList<SelectableCourseSection> _searchedCourseSections = null!;
        [ObservableAsProperty] private IReadOnlyList<SelectableCourseSection> _filteredCourseSections = null!;
        [ObservableAsProperty] private IReadOnlyList<QuickSelectDmhpCourse> _quickSelectCourses = null!;

        private readonly ReadOnlyObservableCollection<CourseBlueprint> _coursesBindable;
        private readonly SourceList<CourseBlueprint> _coursesSourceList;

        public ReadOnlyObservableCollection<CourseBlueprint> Courses => _coursesBindable;
        public ReactiveCommand<Unit, Unit> FocusTextBoxCommand { get; }
        public ReactiveCommand<Unit, Unit> UnfocusTextBoxCommand { get; }
        public ReactiveCommand<Unit, Course> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCoursesCommand { get; }
        public ReactiveCommand<CourseBlueprint, Unit> TreeRemoveCourseCommand { get; }
        public ReactiveCommand<CourseSection, Unit> TreeRemoveSectionCommand { get; }

        public FindCourseViewModel(SchedulingWizardContext context, ICourseCatalogService courseCatalogService,
            ILogger<FindCourseViewModel> logger)
        {
            _coursesSourceList = context.CourseBlueprints;
            _logger = logger;

            #region Source Lists Setup

            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);

            #endregion

            #region Course Search Pipeline

            SearchCommand = ReactiveCommand.CreateFromObservable(() =>
                courseCatalogService.RequestCourseStream(TxtInputCourseKey)
                    .Catch((Exception _) => Observable.Empty<Course>())
            ).DisposeWith(_disposables);

            _searchedCourseHelper = SearchCommand
                .ToProperty(this, nameof(SearchedCourse), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

            #endregion

            #region Section Filtering Pipeline

            _searchedCourseSectionsHelper = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(course => course is null
                    ? (IReadOnlyList<SelectableCourseSection>)[]
                    : course.Sections.Select(s => new SelectableCourseSection(s)).ToList())
                .ToProperty(this, nameof(SearchedCourseSections), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

            _filteredCourseSectionsHelper = this.WhenAnyValue(x => x.ShowOnlyAvailableSections,
                    x => x.SearchedCourseSections,
                    (showOnlyAvailableSections, searchedCourseSections) =>
                        (showOnlyAvailableSections, searchedCourseSections))
                .Where(tuple => tuple.searchedCourseSections != null)
                .Select(tuple => tuple.showOnlyAvailableSections
                    ? tuple.searchedCourseSections
                        .Where(section => section.Item.RemainingStudents > 0).ToList()
                    : tuple.searchedCourseSections)
                .ToProperty(this, nameof(FilteredCourseSections), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

            #endregion

            #region Suggestions Pipeline

            _quickSelectCoursesHelper = this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .Select(query =>
                {
                    if (string.IsNullOrEmpty(query))
                        return Observable.Return<IReadOnlyList<QuickSelectDmhpCourse>>([]);

                    return Observable.Defer(() => courseCatalogService.RequestSuggestionsStream(query))
                        .Select(x => (IReadOnlyList<QuickSelectDmhpCourse>)x.ToList())
                        .SubscribeOn(RxSchedulers.TaskpoolScheduler)
                        .Catch((Exception _) =>
                            Observable.Return<IReadOnlyList<QuickSelectDmhpCourse>>([]));
                })
                .Switch()
                .ToProperty(this, nameof(QuickSelectCourses), initialValue: [],
                    scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

            // Trigger popup when suggestions change and text box is focused
            this.WhenAnyValue(x => x.QuickSelectCourses, x => x.IsTextBoxFocused)
                .Subscribe(_ => TryOpenQuickSelectPopup())
                .DisposeWith(_disposables);

            // Handle user selecting a suggestion
            this.WhenAnyValue(x => x.SelectedQuickSelectDmhpCourse)
                .WhereNotNull()
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(selectedQuickSelectCourse =>
                {
                    TxtInputCourseKey = selectedQuickSelectCourse.CourseCode;
                    SelectedQuickSelectDmhpCourse = null!;
                    IsOpenQuickSelectPopup = false;
                }).DisposeWith(_disposables);

            #endregion

            #region Commands Setup

            FocusTextBoxCommand = ReactiveCommand.Create(() =>
            {
                IsTextBoxFocused = true;
                TryOpenQuickSelectPopup();
            }).DisposeWith(_disposables);

            UnfocusTextBoxCommand =
                ReactiveCommand.Create(() => { IsTextBoxFocused = false; }).DisposeWith(_disposables);

            // Delay closing the popup on lost focus to allow popup clicks to register
            this.WhenAnyValue(x => x.IsTextBoxFocused)
                .Where(focused => !focused)
                .Delay(TimeSpan.FromMilliseconds(150), RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => TryOpenQuickSelectPopup())
                .DisposeWith(_disposables);


            var canAddCourse = this.WhenAnyValue(x => x.SearchedCourseSections,
                searchedCourseSections => searchedCourseSections is not null && searchedCourseSections.Any());

            AddCoursesCommand = ReactiveCommand.Create(AddSelectedSectionsToCart, canAddCourse)
                .DisposeWith(_disposables);

            TreeRemoveCourseCommand = ReactiveCommand.Create<CourseBlueprint>(RemoveCourseFromTree)
                .DisposeWith(_disposables);

            TreeRemoveSectionCommand = ReactiveCommand.Create<CourseSection>(RemoveSectionFromTree)
                .DisposeWith(_disposables);

            #endregion
        }

        private void TryOpenQuickSelectPopup()
        {
            IsOpenQuickSelectPopup = IsTextBoxFocused && (QuickSelectCourses?.Any() ?? false);
        }

        private void AddSelectedSectionsToCart()
        {
            var selected = FilteredCourseSections
                .Where(x => x.IsSelected)
                .Select(x => x.Item)
                .ToList();

            if (selected.Count == 0) return;

            var existingNode = _coursesSourceList.Items.FirstOrDefault(x => x.CoreCourse.Code == SearchedCourse.Code);

            if (existingNode == null)
            {
                var selectedCourseNode = new CourseBlueprint(SearchedCourse, selected);
                _coursesSourceList.Add(selectedCourseNode);
                return;
            }

            existingNode.UpdateSections(list =>
            {
                var newItems = selected.Where(s => list.All(ex => ex.Group != s.Group));

                list.AddRange(newItems);
            });
        }

        private void RemoveCourseFromTree(CourseBlueprint? course)
        {
            if (course == null) return;
            _coursesSourceList.Remove(course);
        }

        private void RemoveSectionFromTree(CourseSection? section)
        {
            if (section == null) return;
            var targetNode = _coursesSourceList.Items
                .FirstOrDefault(n => n.Sections.Contains(section));

            if (targetNode == null) return;

            if (targetNode.Sections.Count == 1 && targetNode.Sections.Contains(section))
            {
                _coursesSourceList.Remove(targetNode);
            }
            else
            {
                targetNode.UpdateSections(list => list.Remove(section));
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _logger.LogDebug("Disposed");
        }
    }
}