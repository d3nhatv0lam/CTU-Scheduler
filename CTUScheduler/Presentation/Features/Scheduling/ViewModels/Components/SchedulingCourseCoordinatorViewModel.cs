using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Presentation.Base;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

public class SchedulingCourseCoordinatorViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<SchedulingCourseViewModel> _scheduledCourses;

    public ReadOnlyObservableCollection<SchedulingCourseViewModel> SchedulingCourses => _scheduledCourses;

    public SchedulingCourseCoordinatorViewModel(IObservableList<SchedulingCourseViewModel> source,
        ILogger<SchedulingCourseCoordinatorViewModel>? logger)
    {
        var sharedConnect = source.Connect()
            .Publish();
        
        // Bind to the main collection for UI display
        sharedConnect
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out _scheduledCourses)
            .Do(_ => logger?.LogDebug("SchedulingCourses updated"))
            .Subscribe()
            .DisposeWith(_disposables);

        // Maintain a filtered collection of "Main" courses for replacements
        sharedConnect
            .AutoRefresh(x => x.IsMainCourse)
            .Filter(x => x.IsMainCourse)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out var mainCourses)
            .Do(_ => logger?.LogDebug("MainCourses updated"))
            .Subscribe()
            .DisposeWith(_disposables);

        // Phân phối danh sách MainCourses cho các môn Alternative
        // đảm bảo bất kỳ môn nào đang hoặc chuyển thành Alternative đều nhận được danh sách options
        sharedConnect
            .AutoRefresh(x => x.IsMainCourse)
            .Filter(x => !x.IsMainCourse)
            .Do(_ => logger?.LogDebug("Updating ReplacementOptions for all Alternative courses"))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    // Khi môn mới được thêm vào hoặc chuyển từ Main -> Alternative
                    if (change.Reason is ListChangeReason.Add or ListChangeReason.Refresh)
                    {
                        change.Item.Current.ReplacementOptions = mainCourses;
                    }
                }
            })
            .DisposeWith(_disposables);
        
        sharedConnect.Connect()
            .DisposeWith(_disposables);
    }

    public IReadOnlyList<IReadOnlyList<Course>> GetGroupedCourses()
    {
        // Nhóm các môn lại dựa trên quan hệ thay thế
        var lookup = SchedulingCourses
            .Where(x => x is { IsMainCourse: false, SelectedReplacement: not null })
            .ToLookup(x => x.SelectedReplacement!.Item, x => x.Item);

        return SchedulingCourses
            .Where(x => x.IsMainCourse)
            .Select(main => (IReadOnlyList<Course>)lookup[main.Item].Prepend(main.Item).ToList())
            .ToList();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}