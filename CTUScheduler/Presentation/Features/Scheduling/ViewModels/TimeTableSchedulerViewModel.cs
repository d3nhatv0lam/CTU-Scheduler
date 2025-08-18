using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.Interfaces;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Scheduling.Models;
using CTUScheduler.Presentation.Scheduling.Interfaces;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels
{
    public class TimeTableSchedulerViewModel: ViewModelBase, IStepViewModel, IDisposable , IActivatableViewModel
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IUserDataService _userDataService;
        private readonly SchedulingCourseViewModel _schedulingCourseVM;
        private readonly SourceList<Course> _coursesSourceList;
        private ReadOnlyObservableCollection<Course> _courseBindable;
        private CancellationTokenSource? _cts;

        
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public ReadOnlyObservableCollection<Course> Courses => _courseBindable;
        public SchedulingCourseViewModel SchedulingCourseVM => _schedulingCourseVM;
        
        public ReactiveCommand<Unit, Unit> GenerateTimeTableCommand { get; protected set; }

        public TimeTableSchedulerViewModel(SourceList<Course> courses)
        {
            _coursesSourceList = courses;
            _userDataService = App.ServiceProvider!.GetRequiredService<IUserDataService>();
            _schedulingCourseVM = new SchedulingCourseViewModel();

            GenerateTimeTableCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    // lấy ma trận các khóa học
                    // đệ quy
                    // xuất ra
                    // chọn tkb
                    // lưu vào UserData
                    await GenerateTimeTable(new List<List<SchedulingCourse>>());
                })
                .DisposeWith(_disposables);
            
            _coursesSourceList.Connect()
                .Bind(out _courseBindable)
                .Subscribe()
                .DisposeWith(_disposables);
            
            this.WhenActivated((CompositeDisposable disposable) =>
            {
                SchedulingCourseVM.MapToSchedulingCourses(Courses);
            });
        }
        

        private async Task GenerateTimeTable(List<List<SchedulingCourse>> sets)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            foreach (var tableData in Combinatorics.CartesianProduct(
                         sets,
                         prefix => true,
                         full => true,
                         _cts.Token))
            {
                
            }
        }
        
        

        public void Dispose()
        {
            SchedulingCourseVM.Dispose();
            _disposables.Dispose();
        }
    }
}
