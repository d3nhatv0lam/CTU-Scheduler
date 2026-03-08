using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.ScheduleService;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using DynamicData;
using ReactiveUI;
using CTUScheduler.Infrastructure.Exel;
//using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using System.Collections.Generic;
using System.IO;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public class TimetableEditorViewModel: TimetableLayoutBaseViewModel, INeedArgs<ScheduleProfile>
{
    private readonly ScheduleProfile _scheduleProfile;
    private readonly ObservableAsPropertyHelper<bool> _isEditing;
    public bool IsEditing => _isEditing.Value;

    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    private readonly IScheduleManager _scheduleManager;
    private readonly IExcelExporterService _excelExporter;

    public TimetableEditorViewModel(
        ScheduleProfile scheduleProfile,
        ICourseQueryService courseQueryService,
        IScheduleManager scheduleManager,
        IExcelExporterService excelExporter)
    {
        ArgumentNullException.ThrowIfNull(scheduleProfile, nameof(scheduleProfile));
        _scheduleProfile = scheduleProfile;
        _scheduleManager = scheduleManager ?? throw new ArgumentNullException(nameof(scheduleManager));
        _excelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));

        Name = _scheduleProfile.Name;
        LastUpdated = _scheduleProfile.LastUpdated;
        
        var savedRef = scheduleProfile.SavedCourseGroupKeys;
        var sharedCourse = courseQueryService.ConnectCourses()
            .Filter(x => savedRef.ContainsKey(x.Code))
            .MergeMany(runtimeCourse =>
            {
                string targetGroup = savedRef[runtimeCourse.Code];
                return runtimeCourse.Sections.Connect()
                    .Filter(s => s.Group == targetGroup)
                    .Transform(section => 
                    {
                        var adapter = new LiveCourseAdapter(runtimeCourse, section);
                        return CreateRenderItem(adapter);
                    })
                    .ChangeKey(_ => Guid.NewGuid());
            })
            .DisposeMany()
            .RemoveKey()
            .AsObservableList()
            .DisposeWith(Disposables);

        VisualizerVM = new TimetableViewModel(sharedCourse.Connect())
            .DisposeWith(Disposables);
        
        sharedCourse.Connect()
            .Transform(item => item.SharedData.Credits)
            .QueryWhenChanged(items => new 
            {
                items.Count, 
                Sum = items.Sum() 
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => 
            {
                SubjectsCount = x.Count;
                TotalCredits = x.Sum;
            })
            .DisposeWith(Disposables);

        sharedCourse.Connect()
            .Skip(1)
            .Throttle(TimeSpan.FromSeconds(1.2))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                LastUpdated = DateTimeOffset.Now;
                _scheduleProfile.LastUpdated = LastUpdated;
            })
            .DisposeWith(Disposables);
        
        StartEditCommand = ReactiveCommand.Create(() => {}).DisposeWith(Disposables);
        SaveCommand = ReactiveCommand.Create(() =>
        {
            _scheduleProfile.Name = this.Name;
            _scheduleProfile.LastUpdated = DateTimeOffset.Now;
            LastUpdated = _scheduleProfile.LastUpdated;
        }).DisposeWith(Disposables);
        CancelCommand = ReactiveCommand.Create(() =>
        {
            Name = _scheduleProfile.Name;
        }).DisposeWith(Disposables);

        _isEditing = Observable.Merge(
                StartEditCommand.Select(_ => true),
                SaveCommand.Select(_ => false),
                CancelCommand.Select(_ => false)
            )
            .ToProperty(this, nameof(IsEditing), initialValue:false, scheduler: RxApp.MainThreadScheduler)
            .DisposeWith(Disposables);

        // --- GÁN ExportToExcelCommand cho TimetableEditorViewModel ---
        ExportToExcelCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                // 1. Lấy dữ liệu gốc từ DataService và ép sang SectionChoice
                var allCourses = _scheduleManager.GetCourseSnapshot();
                var listToExport = new List<SectionChoice>();

                foreach (var kvp in _scheduleProfile.SavedCourseGroupKeys)
                {
                    var courseCode = kvp.Key;
                    var groupName = kvp.Value;

                    var course = allCourses.FirstOrDefault(c => c.Code == courseCode);
                    if (course != null)
                    {
                        var section = course.Sections.FirstOrDefault(s => s.Group == groupName);
                        if (section != null)
                        {
                            listToExport.Add(new SectionChoice(course, section));
                        }
                    }
                }

                if (listToExport.Count == 0) return;

                // 2. Định vị nơi lưu file
                var safeName = string.IsNullOrWhiteSpace(this.Name) ? "TKB" : this.Name.Trim();
                string fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string fullPath = Path.Combine(desktopPath, fileName);

                // 3. Gọi Builder vẽ lưới Excel
                await Task.Run(() =>
                {
                    var wb = CTUScheduler.Infrastructure.Exel.TimetableExcelBuilder.BuildWorkbook(listToExport, "Thời Khóa Biểu");
                    wb.SaveAs(fullPath);
                });

                // 4. Mở thư mục
                new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{fullPath}\"")
                }.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        Debug.WriteLine($"TimetableEditorViewModel ID: {_scheduleProfile.Id}, Name: {Name} disposed");
    }
}