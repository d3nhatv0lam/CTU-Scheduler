using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Utils.Comparers;
using CTUScheduler.Infrastructure.Services.Registration;
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
        private readonly ICourseCatalogService _courseCatalogService;
        private readonly CourseMapper _courseMapper = new();
        private readonly SourceList<CourseUi> _coursesSourceList = new();
        private string _txtInputCourseKey = string.Empty;
        private bool _isOpenQuickSelectPopup;
        private readonly Subject<Unit> _textBoxClickTriggerSubject = new();
        private bool _showOnlyAvailableSections;
        private ObservableAsPropertyHelper<CourseUi> _searchedCourse;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSectionUi>> _searchedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseSectionUi>> _filtedCourseSections;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectCourse>> _quickSelectCourses;
        private QuickSelectCourse _selectedQuickSelectCourse = null!;
        private ReadOnlyObservableCollection<CourseUi> _coursesBindable;


        public SourceList<CourseUi> CoursesSourceList => _coursesSourceList;

        public string TxtInputCourseKey
        {
            get => _txtInputCourseKey;
            set => this.RaiseAndSetIfChanged(ref _txtInputCourseKey, value);
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
        public ReactiveCommand<Unit, Unit> TryOpenPopupCommand { get; }
        public ReactiveCommand<Unit, Unit> ClosePopupCommand { get; }
        public ReactiveCommand<Unit, Course> SearchCommand { get; }
        public ReactiveCommand<SelectableCourseSectionUi, Unit> ChangeSelectStateSectionCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCoursesCommand { get; }
        public ReactiveCommand<CourseUi, Unit> Tree_RemoveCourseCommand { get; }
        public ReactiveCommand<CourseSectionUi, Unit> Tree_RemoveSectionCommand { get; }

        public HandmadeFindCourseViewModel()
        {
            _courseCatalogService = App.ServiceProvider.GetRequiredService<ICourseCatalogService>();
            // TreeView SourceList
            _coursesSourceList.Connect()
                .Bind(out _coursesBindable)
                .Subscribe()
                .DisposeWith(_disposables);

            // Fill & Search Field //
            _quickSelectCourses =this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .DistinctUntilChanged()
                .Select(query =>
                {
                    if (string.IsNullOrEmpty(query)) return Observable.Return(new List<QuickSelectCourse>());;
                    return _courseCatalogService.RequestSuggestionsStream(query)
                        .Catch((Exception _) =>  Observable.Return(new List<QuickSelectCourse>()));
                })
                .Switch()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => new ObservableCollection<QuickSelectCourse>(x))
                .ToProperty(this,
                    nameof(QuickSelectCourses),
                    initialValue: new ObservableCollection<QuickSelectCourse>())
                .DisposeWith(_disposables);


            // không cần Switch vì viết CreateFromObservable sẽ lock nút đến khi xong
            SearchCommand = ReactiveCommand.CreateFromObservable(() =>
                _courseCatalogService.RequestCourseStream(TxtInputCourseKey)
                    .Catch((Exception _) => Observable.Empty<Course>())
            );

            _searchedCourse = SearchCommand
                .Select(course => _courseMapper.ToCourseUi(course))
                .ToProperty(this, nameof(SearchedCourse))
                .DisposeWith(_disposables);

            // OLD - Fill & Search Field //
            // var autoFillStream = this.WhenAnyValue(x => x.TxtInputCourseKey)
            //     .Throttle(TimeSpan.FromMilliseconds(300), RxApp.TaskpoolScheduler)
            //     .DistinctUntilChanged()
            //     .Select(query => (Query: query, IsSearch: false));
            //
            // var manualSearchStream = SearchCommand
            //     .Select(_ => (Query: TxtInputCourseKey, IsSearch: true));
            //
            // autoFillStream.Merge(manualSearchStream)
            //     .DistinctUntilChanged(new LambdaComparer<(string Query, bool IsSearch)>((pre, curr) =>
            //     {
            //         if (curr.IsSearch) return false;
            //         return pre.Query.Equals(curr.Query, StringComparison.OrdinalIgnoreCase);
            //     }))
            //     .Select(req => Observable.FromAsync(async ct => 
            //     {
            //         if (string.IsNullOrEmpty(req.Query))
            //         {
            //             RxApp.MainThreadScheduler.Schedule(() => SearchedCourseSections.Clear());
            //             return;
            //         }
            //         try 
            //         {
            //             await _courseCatalogService.FillQueryAsync(req.Query,ct);
            //             if (req.IsSearch)
            //             {
            //                 await _courseCatalogService.SearchAsync(ct);
            //             }
            //         }
            //         catch 
            //         {
            //            // ignored
            //         }
            //     }))
            //     .Switch()
            //     .Subscribe()
            //     .DisposeWith(_disposables);

            // SUPER OLD - Fill & Search Field //
            // this.WhenAnyValue(x => x.TxtInputCourseKey)
            //     .Throttle(TimeSpan.FromMilliseconds(300))
            //     .ObserveOn(RxApp.TaskpoolScheduler)
            //     .Subscribe( courseData =>
            //     {
            //         _courseCatalogService.FillQueryAsync(courseData);
            //     })
            //     .DisposeWith(_disposables);
            // SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            // {
            //     if (string.IsNullOrEmpty(TxtInputCourseKey))
            //     {
            //         SearchedCourseSections.Clear();
            //         return;
            //     }
            //     try 
            //     {
            //         await _courseCatalogService.FillQueryAsync(TxtInputCourseKey);
            //         await _courseCatalogService.SearchAsync();
            //     }
            //     catch (Exception ex)
            //     {
            //         // ignored
            //     }
            // }).DisposeWith(_disposables);
            //
            // quick select course response
            // _quickSelectCourses = _courseCatalogService.QuickSelectCourseChanges
            //     .Select(x => new ObservableCollection<QuickSelectCourse>(x))
            //     .ToProperty(this, 
            //         nameof(QuickSelectCourses),
            //         initialValue: new ObservableCollection<QuickSelectCourse>(),
            //         scheduler:RxApp.MainThreadScheduler)
            //     .DisposeWith(_disposables);
            

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
            // _searchedCourse = _courseCatalogService.CourseChanges
            //     .WhereNotNull()
            //     .Select(course => _courseMapper.ToCourseUi(course))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .ToProperty(this, nameof(SearchedCourse))
            //     .DisposeWith(_disposables);


            // Course -> UI items
            _searchedCourseSections = this.WhenAnyValue(x => x.SearchedCourse)
                .Select(courseUi =>
                    courseUi is null
                        ? new ObservableCollection<SelectableCourseSectionUi>()
                        : GetSelectableSectionUi(courseUi))
                .ToProperty(this, nameof(SearchedCourseSections))
                .DisposeWith(_disposables);

            // Filtered Course Sections
            _filtedCourseSections = this.WhenAnyValue(x => x.ShowOnlyAvailableSections, x => x.SearchedCourseSections,
                    (showOnlyAvailableSections, searchedCourseSections) =>
                        (showOnlyAvailableSections, searchedCourseSections))
                .Where(tuple => tuple.searchedCourseSections != null)
                .Select(tuple => tuple.showOnlyAvailableSections
                    ? new ObservableCollection<SelectableCourseSectionUi>(
                        tuple.searchedCourseSections.Where(section => section.Item.RemainingStudents > 0))
                    : tuple.searchedCourseSections)
                .ToProperty(this, nameof(FiltedCourseSections))
                .DisposeWith(_disposables);


            this.WhenAnyValue(x => x.QuickSelectCourses)
                .Subscribe(_ => TryOpenQuickSelectPopup())
                .DisposeWith(_disposables);

            TryOpenPopupCommand = ReactiveCommand.Create(TryOpenQuickSelectPopup)
                .DisposeWith(_disposables);

            ClosePopupCommand = ReactiveCommand.Create(() => { IsOpenQuickSelectPopup = false; })
                .DisposeWith(_disposables);


            ChangeSelectStateSectionCommand = ReactiveCommand.Create<SelectableCourseSectionUi>(selectable =>
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

            Tree_RemoveSectionCommand = ReactiveCommand
                .Create<CourseSectionUi>(section => RemoveSectionFromTree(section))
                .DisposeWith(_disposables);
        }

        private void TryOpenQuickSelectPopup()
        {
            if ((QuickSelectCourses?.Any()).GetValueOrDefault())
                IsOpenQuickSelectPopup = true;
            else
                IsOpenQuickSelectPopup = false;
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