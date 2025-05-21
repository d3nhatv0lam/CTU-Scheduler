using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable.Interfaces;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable.Components;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable
{
    public class HandmadeFindCourseViewModel : ViewModelBase, IDisposable, IRoutableViewModel, IStepViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly ICTUWebDriverService _ctuWebDriverService;
        private string _txtInputCourseKey = string.Empty;
        private bool _isTxtInputCourseKeyFocused = false;
        private ObservableAsPropertyHelper<Course> _course;
        private ObservableAsPropertyHelper<ObservableCollection<SelectableCourseData>> _coursesCatalog;
        private ObservableAsPropertyHelper<bool> _isQuickSelectPopupOpened;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectCourse>> _quickSelectCourses;
        private QuickSelectCourse _selectedQuickSelectCourse;

        public string? UrlPathSegment => "Handmade_Find_Course";

        public IScreen HostScreen { get; }
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

        public Course Course => _course.Value;
        public ObservableCollection<SelectableCourseData> CoursesCatalog => _coursesCatalog.Value;
        public ObservableCollection<QuickSelectCourse> QuickSelectCourses => _quickSelectCourses.Value;
        public QuickSelectCourse SelectedQuickSelectCourse
        {
            get => _selectedQuickSelectCourse;
            set => this.RaiseAndSetIfChanged(ref _selectedQuickSelectCourse, value);
        }

        public ReactiveCommand<Unit,Unit> SearchCommand { get; }

        

        public HandmadeFindCourseViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _ctuWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();
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
                                 (tBoxFocus, qCourses) => qCourses != null? tBoxFocus && qCourses.Any(): false)
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

            // course response
            _course = _ctuWebDriverService.CourseCatalogResponse
                .ToProperty(this, nameof(Course))
                .DisposeWith(_disposables);

            // Course -> UI items
            _coursesCatalog = this.WhenAnyValue(x => x.Course)
                .WhereNotNull()
                .Select(course => ToSelectableCourseCatalogs(course))
                .ToProperty(this, nameof(CoursesCatalog))
                .DisposeWith(_disposables);


            SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrEmpty(TxtInputCourseKey))
                {
                    CoursesCatalog.Clear();
                    return;
                }
                await _ctuWebDriverService.SearchCourse(TxtInputCourseKey);
            }).DisposeWith(_disposables);
        }

        private ObservableCollection<SelectableCourseData> ToSelectableCourseCatalogs(Course course)
        {
            var items = new ObservableCollection<SelectableCourseData>();
            foreach (var courseGroup in course.CourseDatas)
            {
                items.Add(SelectableCourseData.ToSelectableCourseData(courseGroup));
            }
            return items;
        }



        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
