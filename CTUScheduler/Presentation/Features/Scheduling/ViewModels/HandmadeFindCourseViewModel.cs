using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.JavaScript;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class HandmadeFindCourseViewModel : ViewModelBase, IDisposable, IStepViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _ctuWebDriverService;
        private readonly SourceList<Course> _coursesSourceList = new SourceList<Course>();
        private string _txtInputCourseKey = string.Empty;
        private bool _isTxtInputCourseKeyFocused = false;
        private bool _showOnlyAvailableSections = false;
        private ObservableAsPropertyHelper<Course> _searchedCourse;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseData>> _searchedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseData>> _filtedCourseSections;
        private ObservableAsPropertyHelper<bool> _isQuickSelectPopupOpened;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectCourse>> _quickSelectCourses;
        private QuickSelectCourse _selectedQuickSelectCourse;
        private ReadOnlyObservableCollection<Course> _coursesBindable;
        
        
        public  SourceList<Course> CoursesSourceList => _coursesSourceList;
        public string TxtInputCourseKey
        {
            get => _txtInputCourseKey;
            set => this.RaiseAndSetIfChanged(ref _txtInputCourseKey, value);
        }
        public bool IsTxtInputCourseKeyFocused
        {
            get => _isTxtInputCourseKeyFocused;
            set => this.RaiseAndSetIfChanged(ref _isTxtInputCourseKeyFocused, value);
        }
        public bool IsQuickSelectPopupOpened => _isQuickSelectPopupOpened.Value;
        public bool ShowOnlyAvailableSections 
        {             
            get => _showOnlyAvailableSections;
            set => this.RaiseAndSetIfChanged(ref _showOnlyAvailableSections, value);
        }
        public Course SearchedCourse => _searchedCourse.Value;
        public ObservableCollection<SelectableCourseData> SearchedCourseSections => _searchedCourseSections.Value;
        public ObservableCollection<SelectableCourseData> FiltedCourseSections => _filtedCourseSections.Value;
        public ObservableCollection<QuickSelectCourse> QuickSelectCourses => _quickSelectCourses.Value;
        public QuickSelectCourse SelectedQuickSelectCourse
        {
            get => _selectedQuickSelectCourse;
            set => this.RaiseAndSetIfChanged(ref _selectedQuickSelectCourse, value);
        }
        public ReadOnlyObservableCollection<Course> Courses => _coursesBindable;
        public ReactiveCommand<Unit,Unit> SearchCommand { get; }
        public ReactiveCommand<Unit,Unit> AddCoursesCommand { get; }
        public ReactiveCommand<Course,Unit> Tree_RemoveCourseCommand { get; }
        public ReactiveCommand<CourseData,Unit> Tree_RemoveSectionCommand { get; }
        
        public HandmadeFindCourseViewModel()
        {
            _ctuWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

            // TreeView SourceList
            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            // quick select course
            this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Where(x => !string.IsNullOrEmpty(x))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(async courseData =>
                {
                    await _ctuWebDriverService.FillCourseKey(courseData);
                })
                .DisposeWith(_disposables);

            // quick select course response
            _quickSelectCourses = _ctuWebDriverService.CourseCatalogQuickSelectResponse
              .ToProperty(this, nameof(QuickSelectCourses))
              .DisposeWith(_disposables);

            // quick select course popup opened expression
            _isQuickSelectPopupOpened =
               this.WhenAnyValue(x => x.IsTxtInputCourseKeyFocused,
                                 x => x.QuickSelectCourses,
                                 (tBoxFocus, qCourses) => qCourses != null? tBoxFocus && qCourses.Any() : false)
              .ToProperty(this, nameof(IsQuickSelectPopupOpened))
              .DisposeWith(_disposables);

            // Selected QuickSelectCourse do
            this.WhenAnyValue(x => x.SelectedQuickSelectCourse)
                .WhereNotNull()
                .Subscribe(selectedQuickSelectCourse =>
                {
                    // set course key
                    TxtInputCourseKey = selectedQuickSelectCourse.CourseCode;
                }).DisposeWith(_disposables);

            // current course response
            _searchedCourse = _ctuWebDriverService.CourseCatalogResponse
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(SearchedCourse))
                .DisposeWith(_disposables);

            // Course -> UI items
            _searchedCourseSections = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(course => course == null ? new ObservableCollection<SelectableCourseData>():ToSelectableCourseCatalogs(course))
                .ToProperty(this, nameof(SearchedCourseSections))
                .DisposeWith(_disposables);
            
            // Filtered Course Sections
            _filtedCourseSections = this.WhenAnyValue(x => x.ShowOnlyAvailableSections, x => x.SearchedCourseSections, (showOnlyAvailableSections, searchedCourseSections) => (showOnlyAvailableSections, searchedCourseSections))
                        .Where(tuple => tuple.searchedCourseSections != null && tuple.searchedCourseSections.Any())
                        .Select(tuple => tuple.showOnlyAvailableSections ?  new ObservableCollection<SelectableCourseData>(tuple.searchedCourseSections.Where(section => section.Item.RemainingStudents > 0)): tuple.searchedCourseSections)
                        .ToProperty(this, nameof(FiltedCourseSections))
                        .DisposeWith(_disposables);

            SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrEmpty(TxtInputCourseKey))
                {
                    SearchedCourseSections.Clear();
                    return;
                }
                await _ctuWebDriverService.SearchCourse(TxtInputCourseKey);
            }).DisposeWith(_disposables);

            var canAddCourse = this.WhenAnyValue(x => x.SearchedCourseSections, searchedCourseSections => searchedCourseSections != null && searchedCourseSections.Any());
            AddCoursesCommand = ReactiveCommand.Create(() =>
            {
                var selectedSections = FiltedCourseSections
                                    .Where(x => x.IsSelected)
                                    .Select(x => x.Item)
                                    .ToList();

                if (!selectedSections.Any()) return;

                var treeCourseNode = Courses.FirstOrDefault(x => x.Code == SearchedCourse.Code);
                
                if (treeCourseNode == null)
                {
                    treeCourseNode = SearchedCourse.CloneWithNewCourseDatas(selectedSections);
                    _coursesSourceList.Add(treeCourseNode);
                    return;
                }
                
                Comparer<CourseData> comparer = Comparer<CourseData>.Create((x, y) => x.Key.CompareTo(y.Key));
                foreach (var section in selectedSections)
                {
                    int index = treeCourseNode.Sections.BinarySearch(section, comparer);
                    if (index < 0)
                    {
                        index = ~index; // Get the index where the item should be inserted
                        treeCourseNode.Sections.Insert(index, section);
                    }
                }
            }, canAddCourse).DisposeWith(_disposables);


            Tree_RemoveCourseCommand = ReactiveCommand.Create<Course>(course => RemoveCourseFromTree(course))
                                       .DisposeWith(_disposables);

            Tree_RemoveSectionCommand = ReactiveCommand.Create<CourseData>(section => RemoveSectionFromTree(section))
                                        .DisposeWith(_disposables);
        }

        private ObservableCollection<SelectableCourseData> ToSelectableCourseCatalogs(Course course)
        {
            var items = new ObservableCollection<SelectableCourseData>();
            foreach (var courseGroup in course.Sections)
            {
                items.Add(SelectableCourseData.ToSelectableCourseData(courseGroup));
            }
            return items;
        }

        private void RemoveCourseFromTree(Course? course)
        {
            if (course == null) return;
            _coursesSourceList.Remove(course);
        }

        private void RemoveSectionFromTree(CourseData? section)
        {
            if (section == null) return;
            var course = Courses.FirstOrDefault(x => x.Code == section.Code);
            if (course == null) return;
            course.Sections.Remove(section);
            if (!course.Sections.Any()) 
                RemoveCourseFromTree(course);
        }

        public void Dispose()
        {
            _coursesSourceList.Dispose();
            _disposables.Dispose();
        }

        
    }
}
