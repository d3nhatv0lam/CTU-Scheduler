using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Raw;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Presentation.ViewModels.Base;
using CTUScheduler.Presentation.ViewModels.CoursePage.AddScheduleTable.Interfaces;
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
        private ObservableCollection<SelectableItem<Course>> _courses;
        private ObservableAsPropertyHelper<bool> _isQuickSelectPopupOpened;
        private ObservableAsPropertyHelper<ObservableCollection<QuickSelectCourse>> _quickSelectCourses;

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
        public ObservableCollection<SelectableItem<Course>> Courses
        {
            get => _courses;
            set => this.RaiseAndSetIfChanged(ref _courses, value);
        }
        public ObservableCollection<QuickSelectCourse> QuickSelectCourses => _quickSelectCourses.Value;
      

        public ReactiveCommand<Unit,Unit> SearchCommand { get; }

        

        public HandmadeFindCourseViewModel(IScreen hostScreen)
        {
            HostScreen = hostScreen;
            _ctuWebDriverService = App.ServiceProvider!.GetRequiredService<ICTUWebDriverService>();

  
            // quick select course
            this.WhenAnyValue(x => x.TxtInputCourseKey)
                .Throttle(TimeSpan.FromMilliseconds(500))
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

            _isQuickSelectPopupOpened =
               this.WhenAnyValue(x => x.IsTxtInputCourseKeyFocused,
                                 x => x.QuickSelectCourses,
                                 (tBoxFocus, qCourses) => qCourses != null? tBoxFocus && qCourses.Any(): false)
              .ToProperty(this, nameof(IsQuickSelectPopupOpened))
              .DisposeWith(_disposables);

            SearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrEmpty(TxtInputCourseKey))
                {
                    return;
                }
            }).DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
