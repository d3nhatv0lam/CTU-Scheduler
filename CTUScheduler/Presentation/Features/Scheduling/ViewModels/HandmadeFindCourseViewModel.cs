using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.WebDriver;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Features.Scheduling.Shared.Interfaces;
using CTUScheduler.Presentation.Shared.Extensions;
using CTUScheduler.Presentation.Shared.Mappers;
using CTUScheduler.Presentation.Shared.Models.Academic;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class HandmadeFindCourseViewModel : ViewModelBase, IDisposable, IStepViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _ctuWebDriverService;
        private readonly CourseMapper _courseMapper = new();
        private readonly SourceList<CourseUi> _coursesSourceList = new();
        private string _txtInputCourseKey = string.Empty;
        private bool _isTextBoxSearchFocused;
        private bool _isOpenQuickSelectPopup;
        private Subject<Unit> _textBoxClickTriggerSubject = new();
        private bool _showOnlyAvailableSections;
        private ObservableAsPropertyHelper<CourseUi> _searchedCourse;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSectionUi>> _searchedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSectionUi>> _filtedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectCourse>> _quickSelectCourses;
        private QuickSelectCourse _selectedQuickSelectCourse = null!;
        private ReadOnlyObservableCollection<CourseUi> _coursesBindable;
        
        
        public  SourceList<CourseUi> CoursesSourceList => _coursesSourceList;
        public string TxtInputCourseKey
        {
            get => _txtInputCourseKey;
            set => this.RaiseAndSetIfChanged(ref _txtInputCourseKey, value);
        }
        public bool IsTextBoxSearchFocused
        {
            get => _isTextBoxSearchFocused;
            set => this.RaiseAndSetIfChanged(ref _isTextBoxSearchFocused, value);
        }
        public bool IsOpenQuickSelectPopup
        {
            get => _isOpenQuickSelectPopup;
            set => this.RaiseAndSetIfChanged(ref _isOpenQuickSelectPopup, value);           
        }
        
        public bool ShowOnlyAvailableSections 
        {             
            get => _showOnlyAvailableSections;
            set => this.RaiseAndSetIfChanged(ref _showOnlyAvailableSections, value);
        }
        public CourseUi SearchedCourse => _searchedCourse.Value;
        public ObservableCollection<SelectableCourseSectionUi> SearchedCourseSections => _searchedCourseSections.Value;
        public ObservableCollection<SelectableCourseSectionUi> FiltedCourseSections => _filtedCourseSections.Value;
        public ObservableCollection<QuickSelectCourse> QuickSelectCourses => _quickSelectCourses.Value;
        public QuickSelectCourse SelectedQuickSelectCourse
        {
            get => _selectedQuickSelectCourse;
            set => this.RaiseAndSetIfChanged(ref _selectedQuickSelectCourse, value);
        }
        public ReadOnlyObservableCollection<CourseUi> Courses => _coursesBindable;
        public ReactiveCommand<Unit,Unit> OpenPopupCommand { get; }
        public ReactiveCommand<Unit,Unit> ClosePopupCommand { get; }
        public ReactiveCommand<Unit,Unit> SearchCommand { get; }
        public ReactiveCommand<Unit,Unit> AddCoursesCommand { get; }
        public ReactiveCommand<CourseUi,Unit> Tree_RemoveCourseCommand { get; }
        public ReactiveCommand<CourseSectionUi,Unit> Tree_RemoveSectionCommand { get; }
        
        public HandmadeFindCourseViewModel()
        {
            _ctuWebDriverService = App.ServiceProvider.GetRequiredService<ICTUWebDriverService>();

            // TreeView SourceList
            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            // quick select course
            this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe( courseData =>
                {
                    _ctuWebDriverService.FillCourseKey(courseData);
                })
                .DisposeWith(_disposables);

            // quick select course response
            _quickSelectCourses = _ctuWebDriverService.CourseCatalogQuickSelectResponse
              .ToProperty(this, nameof(QuickSelectCourses),scheduler:RxApp.MainThreadScheduler)
              .DisposeWith(_disposables);
            

            // Selected QuickSelectCourse do
            this.WhenAnyValue(x => x.SelectedQuickSelectCourse)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(selectedQuickSelectCourse =>
                {
                    // set course key
                    TxtInputCourseKey = selectedQuickSelectCourse.CourseCode;
                    SelectedQuickSelectCourse = null!;
                    IsOpenQuickSelectPopup = false;
                }).DisposeWith(_disposables);

            // current course response
            _searchedCourse = _ctuWebDriverService.CourseCatalogResponse
                .WhereNotNull()
                .Select(course => _courseMapper.ToCourseUi(course))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(SearchedCourse))
                .DisposeWith(_disposables);

            // Course -> UI items
            _searchedCourseSections = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(courseUi => courseUi is null ? new ObservableCollection<SelectableCourseSectionUi>() : GetSelectableSectionUi(courseUi))
                .ToProperty(this, nameof(SearchedCourseSections))
                .DisposeWith(_disposables);
            
            // Filtered Course Sections
            _filtedCourseSections = this.WhenAnyValue(x => x.ShowOnlyAvailableSections, x => x.SearchedCourseSections, (showOnlyAvailableSections, searchedCourseSections) => (showOnlyAvailableSections, searchedCourseSections))
                        .Where(tuple => tuple.searchedCourseSections != null)
                        .Select(tuple => tuple.showOnlyAvailableSections 
                            ? new ObservableCollection<SelectableCourseSectionUi>(tuple.searchedCourseSections.Where(section => section.Item.RemainingStudents > 0))
                            : tuple.searchedCourseSections)
                        .ToProperty(this, nameof(FiltedCourseSections))
                        .DisposeWith(_disposables);

            var popupTrigger = _textBoxClickTriggerSubject
                .CombineLatest(
                    this.WhenAnyValue(x => x.IsTextBoxSearchFocused),
                    this.WhenAnyValue(x => x.QuickSelectCourses),
                    (trigger, focused, courses) => (trigger, focused, courses))
                .Select(x => (x.focused, x.courses))
                .Throttle(TimeSpan.FromMilliseconds(50)); // Chống nhiễu

            popupTrigger
                .Subscribe(x =>
                {
                    var (focused, courses) = x;
                    
                    if (!focused) IsOpenQuickSelectPopup = false;
                    
                    if (focused && courses?.Any() == true)
                        IsOpenQuickSelectPopup = true;
                })
                .DisposeWith(_disposables);
            
            
            OpenPopupCommand = ReactiveCommand.Create(() =>
            {
                if (QuickSelectCourses?.Any() == false) return;
                
                if (IsTextBoxSearchFocused && !IsOpenQuickSelectPopup)
                    IsOpenQuickSelectPopup = true;
            }).DisposeWith(_disposables);
            
            ClosePopupCommand = ReactiveCommand.Create(() =>
                {
                    IsOpenQuickSelectPopup = false;
                })
                .DisposeWith(_disposables);

            SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrEmpty(TxtInputCourseKey))
                {
                    SearchedCourseSections.Clear();
                    return;
                }
                await Task.WhenAll(
                    Task.Delay(500),
                    _ctuWebDriverService.SearchCourse(TxtInputCourseKey));
            }).DisposeWith(_disposables);

            var canAddCourse = this.WhenAnyValue(x => x.SearchedCourseSections, searchedCourseSections => searchedCourseSections != null && searchedCourseSections.Any());
            AddCoursesCommand = ReactiveCommand.Create(() =>
            {
                var selectedSections = FiltedCourseSections
                                    .Where(x => x.IsSelected)
                                    .Select(x => x.Item)
                                    .ToList();

                if (selectedSections.Count == 0) return;

                var treeCourseNode = Courses.FirstOrDefault(x => x.Code == SearchedCourse.Code);
                
                if (treeCourseNode == null)
                {
                    treeCourseNode = SearchedCourse.CloneWithNewCourseSections(selectedSections);
                    _coursesSourceList.Add(treeCourseNode);
                    return;
                }
                
                var comparer = Comparer<CourseSectionUi>.Create((x, y) => x.Key.CompareTo(y.Key));
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


            Tree_RemoveCourseCommand = ReactiveCommand.Create<CourseUi>(course => RemoveCourseFromTree(course))
                                       .DisposeWith(_disposables);

            Tree_RemoveSectionCommand = ReactiveCommand.Create<CourseSectionUi>(section => RemoveSectionFromTree(section))
                                        .DisposeWith(_disposables);
        }

        

        private ObservableCollection<SelectableCourseSectionUi> GetSelectableSectionUi(CourseUi course)
        {
            var items = new ObservableCollection<SelectableCourseSectionUi>();
            foreach (var courseSectionUi in course.Sections)
            {
                items.Add(new SelectableCourseSectionUi(courseSectionUi));
            }
            return items;
        }
        
        private void RemoveCourseFromTree(CourseUi? course)
        {
            if (course == null) return;
            _coursesSourceList.Remove(course);
        }

        private void RemoveSectionFromTree(CourseSectionUi? section)
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
            _textBoxClickTriggerSubject.Dispose();
            _disposables.Dispose();
        }
    }
}
