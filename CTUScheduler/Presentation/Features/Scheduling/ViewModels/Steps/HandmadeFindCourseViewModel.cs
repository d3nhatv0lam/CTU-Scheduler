using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models.Context;
using CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
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

        private ObservableAsPropertyHelper<Course> _searchedCourse;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSection>> _searchedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSection>> _filtedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectDmhpCourse>> _quickSelectCourses;

        private QuickSelectDmhpCourse _selectedQuickSelectDmhpCourse = null!;

        private readonly Subject<Unit> _textBoxClickTriggerSubject = new();
        private ReadOnlyObservableCollection<SelectedCourse> _coursesBindable;
        private readonly SourceList<SelectedCourse> _coursesSourceList;


        public Course SearchedCourse => _searchedCourse.Value;
        public ObservableCollection<SelectableCourseSection> SearchedCourseSections => _searchedCourseSections.Value;
        public ObservableCollection<SelectableCourseSection> FiltedCourseSections => _filtedCourseSections.Value;
        public ObservableCollection<QuickSelectDmhpCourse> QuickSelectCourses => _quickSelectCourses.Value;

        public QuickSelectDmhpCourse SelectedQuickSelectDmhpCourse
        {
            get => _selectedQuickSelectDmhpCourse;
            set => this.RaiseAndSetIfChanged(ref _selectedQuickSelectDmhpCourse, value);
        }

        public ReadOnlyObservableCollection<SelectedCourse> Courses => _coursesBindable;
        public ReactiveCommand<Unit, Unit> FocusTextBoxCommand { get; }
        public ReactiveCommand<Unit, Unit> UnfocusTextBoxCommand { get; }
        public ReactiveCommand<Unit, Course> SearchCommand { get; }
        public ReactiveCommand<SelectableCourseSection, Unit> ChangeSelectStateSectionCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCoursesCommand { get; }
        public ReactiveCommand<SelectedCourse, Unit> Tree_RemoveCourseCommand { get; }
        public ReactiveCommand<CourseSection, Unit> Tree_RemoveSectionCommand { get; }

        public HandmadeFindCourseViewModel(SchedulingWizardContext context, ICourseCatalogService courseCatalogService)
        {
            _courseCatalogService = courseCatalogService;
            _coursesSourceList = context.SelectedCourses;

            // TreeView SourceList
            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);

            // Fill & Search Field //
            _quickSelectCourses = this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .Select(query =>
                {
                    if (string.IsNullOrEmpty(query)) return Observable.Return(new List<QuickSelectDmhpCourse>());
                    return _courseCatalogService.RequestSuggestionsStream(query)
                        .SubscribeOn(RxApp.TaskpoolScheduler)
                        .Catch((Exception _) => Observable.Return(new List<QuickSelectDmhpCourse>()));
                })
                .Switch()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => new ObservableCollection<QuickSelectDmhpCourse>(x))
                .ToProperty(this,
                    nameof(QuickSelectCourses),
                    initialValue: new ObservableCollection<QuickSelectDmhpCourse>())
                .DisposeWith(_disposables);


            // không cần Switch vì viết CreateFromObservable sẽ lock nút đến khi xong
            SearchCommand = ReactiveCommand.CreateFromObservable(() =>
                _courseCatalogService.RequestCourseStream(TxtInputCourseKey)
                    .Catch((Exception _) => Observable.Empty<Course>())
            );

            _searchedCourse = SearchCommand
                .ToProperty(this, nameof(SearchedCourse), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);


            // Selected QuickSelectCourse do
            this.WhenAnyValue(x => x.SelectedQuickSelectDmhpCourse)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(selectedQuickSelectCourse =>
                {
                    // set course key
                    TxtInputCourseKey = selectedQuickSelectCourse.CourseCode;
                    SelectedQuickSelectDmhpCourse = null!;
                    IsOpenQuickSelectPopup = false;
                }).DisposeWith(_disposables);


            // Course -> UI items
            _searchedCourseSections = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(course => course is null
                    ? new ObservableCollection<SelectableCourseSection>()
                    : new ObservableCollection<SelectableCourseSection>(
                        course.Sections.Select(s => new SelectableCourseSection(s))))
                .ToProperty(this, nameof(SearchedCourseSections), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);

            // Filtered Course Sections
            _filtedCourseSections = this.WhenAnyValue(x => x.ShowOnlyAvailableSections, x => x.SearchedCourseSections,
                    (showOnlyAvailableSections, searchedCourseSections) =>
                        (showOnlyAvailableSections, searchedCourseSections))
                .Where(tuple => tuple.searchedCourseSections != null)
                .Select(tuple => tuple.showOnlyAvailableSections
                    ? new ObservableCollection<SelectableCourseSection>(
                        tuple.searchedCourseSections.Where(section => section.Item.RemainingStudents > 0))
                    : tuple.searchedCourseSections)
                .ToProperty(this, nameof(FiltedCourseSections), scheduler: RxApp.MainThreadScheduler)
                .DisposeWith(_disposables);


            this.WhenAnyValue(x => x.QuickSelectCourses, x => x.IsTextBoxFocused)
                .Subscribe(_ => TryOpenQuickSelectPopup())
                .DisposeWith(_disposables);

            FocusTextBoxCommand = ReactiveCommand.Create(() =>
            {
                IsTextBoxFocused = true;
                TryOpenQuickSelectPopup();
            }).DisposeWith(_disposables);

            UnfocusTextBoxCommand = ReactiveCommand.Create(() =>
            {
                IsTextBoxFocused = false;
                IsOpenQuickSelectPopup = false;
            }).DisposeWith(_disposables);


            ChangeSelectStateSectionCommand = ReactiveCommand.Create<SelectableCourseSection>(selectable =>
                {
                    selectable.IsSelected = !selectable.IsSelected;
                })
                .DisposeWith(_disposables);

            var canAddCourse = this.WhenAnyValue(x => x.SearchedCourseSections,
                searchedCourseSections => searchedCourseSections != null && searchedCourseSections.Any());
            AddCoursesCommand = ReactiveCommand.Create(() =>
            {
                var selectedSections = FiltedCourseSections
                    .Where(x => x.IsSelected)
                    .Select(x => x.Item)
                    .ToList();

                if (selectedSections.Count == 0) return;

                var existingNode =
                    _coursesSourceList.Items.FirstOrDefault(x => x.CoreCourse.Code == SearchedCourse.Code);

                if (existingNode == null)
                {
                    var selectedCourseNode = new SelectedCourse(SearchedCourse, selectedSections);
                    _coursesSourceList.Add(selectedCourseNode);
                    return;
                }

                foreach (var section in selectedSections)
                {
                    if (!existingNode.Sections.Contains(section))
                        existingNode.Sections.Add(section);
                }

                var sorted = existingNode.Sections.OrderBy(s => s.Group).ToList();
                existingNode.Sections.Clear();
                foreach (var s in sorted) existingNode.Sections.Add(s);
            }, canAddCourse).DisposeWith(_disposables);


            Tree_RemoveCourseCommand = ReactiveCommand.Create<SelectedCourse>(course => RemoveCourseFromTree(course))
                .DisposeWith(_disposables);

            Tree_RemoveSectionCommand = ReactiveCommand
                .Create<CourseSection>(section => RemoveSectionFromTree(section))
                .DisposeWith(_disposables);
        }

        private void TryOpenQuickSelectPopup()
        {
            if ((QuickSelectCourses?.Any()).GetValueOrDefault() && IsTextBoxFocused)
                IsOpenQuickSelectPopup = true;
            else
                IsOpenQuickSelectPopup = false;
        }


        private void RemoveCourseFromTree(SelectedCourse? course)
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
            _textBoxClickTriggerSubject.Dispose();
            _disposables.Dispose();
        }
    }
}