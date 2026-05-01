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
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Steps
{
    public partial class HandmadeFindCourseViewModel : ViewModelBase, IDisposable, IWizardStep,
        INeedArgs<SchedulingWizardContext>
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICourseCatalogService _courseCatalogService;

        [Reactive] private string _txtInputCourseKey = string.Empty;
        [Reactive] private bool _isOpenQuickSelectPopup;
        [Reactive] private bool _showOnlyAvailableSections;
        [Reactive] private bool _isTextBoxFocused;
        [Reactive] private QuickSelectDmhpCourse _selectedQuickSelectDmhpCourse = null!;

        [ObservableAsProperty(ReadOnly = false)]
        private Course _searchedCourse = null!;

        [ObservableAsProperty(ReadOnly = false)]
        private IReadOnlyList<SelectableCourseSection> _searchedCourseSections = null!;

        [ObservableAsProperty(ReadOnly = false)]
        private IReadOnlyList<SelectableCourseSection> _filteredCourseSections = null!;

        [ObservableAsProperty(ReadOnly = false)]
        private IReadOnlyList<QuickSelectDmhpCourse> _quickSelectCourses = null!;


        private ReadOnlyObservableCollection<CourseBlueprint> _coursesBindable;
        private readonly SourceList<CourseBlueprint> _coursesSourceList;

        public ReadOnlyObservableCollection<CourseBlueprint> Courses => _coursesBindable;
        public ReactiveCommand<Unit, Unit> FocusTextBoxCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> UnfocusTextBoxCommand { get; private set; }
        public ReactiveCommand<Unit, Course> SearchCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> AddCoursesCommand { get; private set; }
        public ReactiveCommand<CourseBlueprint, Unit> Tree_RemoveCourseCommand { get; private set; }
        public ReactiveCommand<CourseSection, Unit> Tree_RemoveSectionCommand { get; private set; }

        public HandmadeFindCourseViewModel(SchedulingWizardContext context, ICourseCatalogService courseCatalogService)
        {
            _courseCatalogService = courseCatalogService;
            _coursesSourceList = context.CourseBlueprints;

            SetupTreeSourceList();
            SetupCourseSearchPipeline();
            SetupSectionFilteringPipeline();
            SetupSuggestionsPipeline();
            SetupCommands();
        }

        private void SetupTreeSourceList()
        {
            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);
        }

        private void SetupSuggestionsPipeline()
        {
            _quickSelectCoursesHelper = this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .Select(query =>
                {
                    if (string.IsNullOrEmpty(query))
                        return Observable.Return<IReadOnlyList<QuickSelectDmhpCourse>>([]);

                    return Observable.Defer(() => _courseCatalogService.RequestSuggestionsStream(query))
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
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(selectedQuickSelectCourse =>
                {
                    TxtInputCourseKey = selectedQuickSelectCourse.CourseCode;
                    SelectedQuickSelectDmhpCourse = null!;
                    IsOpenQuickSelectPopup = false;
                }).DisposeWith(_disposables);
        }

        private void SetupCourseSearchPipeline()
        {
            SearchCommand = ReactiveCommand.CreateFromObservable(() =>
                _courseCatalogService.RequestCourseStream(TxtInputCourseKey)
                    .Catch((Exception _) => Observable.Empty<Course>())
            );

            _searchedCourseHelper = SearchCommand
                .ToProperty(this, nameof(SearchedCourse), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
        }

        private void SetupSectionFilteringPipeline()
        {
            _searchedCourseSectionsHelper = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(course => course is null
                    ? (IReadOnlyList<SelectableCourseSection>)Array.Empty<SelectableCourseSection>()
                    : course.Sections.Select(s => new SelectableCourseSection(s)).ToList())
                .ToProperty(this, nameof(SearchedCourseSections), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            _filteredCourseSectionsHelper = this.WhenAnyValue(x => x.ShowOnlyAvailableSections,
                    x => x.SearchedCourseSections,
                    (showOnlyAvailableSections, searchedCourseSections) =>
                        (showOnlyAvailableSections, searchedCourseSections))
                .Where(tuple => tuple.searchedCourseSections != null)
                .Select(tuple => tuple.showOnlyAvailableSections
                    ? (IReadOnlyList<SelectableCourseSection>)tuple.searchedCourseSections
                        .Where(section => section.Item.RemainingStudents > 0).ToList()
                    : tuple.searchedCourseSections)
                .ToProperty(this, nameof(FilteredCourseSections), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);
        }

        private void SetupCommands()
        {
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
                .Delay(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .Subscribe(_ => TryOpenQuickSelectPopup())
                .DisposeWith(_disposables);


            var canAddCourse = this.WhenAnyValue(x => x.SearchedCourseSections,
                searchedCourseSections => searchedCourseSections is not null && searchedCourseSections.Any());

            AddCoursesCommand = ReactiveCommand.Create(AddSelectedSectionsToCart, canAddCourse)
                .DisposeWith(_disposables);

            Tree_RemoveCourseCommand = ReactiveCommand.Create<CourseBlueprint>(RemoveCourseFromTree)
                .DisposeWith(_disposables);

            Tree_RemoveSectionCommand = ReactiveCommand.Create<CourseSection>(RemoveSectionFromTree)
                .DisposeWith(_disposables);
        }

        private void AddSelectedSectionsToCart()
        {
            var selectedSections = FilteredCourseSections
                .Where(x => x.IsSelected)
                .Select(x => x.Item)
                .ToList();

            if (selectedSections.Count == 0) return;

            var existingNode = _coursesSourceList.Items.FirstOrDefault(x => x.CoreCourse.Code == SearchedCourse.Code);

            if (existingNode == null)
            {
                var selectedCourseNode = new CourseBlueprint(SearchedCourse, selectedSections);
                _coursesSourceList.Add(selectedCourseNode);
                return;
            }

            foreach (var section in selectedSections)
            {
                if (existingNode.Sections.All(s => s.Group != section.Group))
                    existingNode.Sections.Add(section);
            }

            var sorted = existingNode.Sections.OrderBy(s => s.Group).ToList();
            existingNode.Sections.Clear();
            foreach (var s in sorted) existingNode.Sections.Add(s);
        }

        private void TryOpenQuickSelectPopup()
        {
            if ((QuickSelectCourses?.Any()).GetValueOrDefault() && IsTextBoxFocused)
                IsOpenQuickSelectPopup = true;
            else
                IsOpenQuickSelectPopup = false;
        }


        private void RemoveCourseFromTree(CourseBlueprint? course)
        {
            if (course == null) return;
            _coursesSourceList.Remove(course);
        }

        private void RemoveSectionFromTree(CourseSection section)
        {
            if (section == null) return;
            var targetNode = _coursesSourceList.Items.FirstOrDefault(n => n.Sections.Contains(section));
            if (targetNode == null) return;
            targetNode.Sections.Remove(section);
            if (!targetNode.Sections.Any())
                RemoveCourseFromTree(targetNode);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}