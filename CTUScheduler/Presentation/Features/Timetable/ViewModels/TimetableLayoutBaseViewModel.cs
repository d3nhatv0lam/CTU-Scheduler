using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using CTUScheduler.AppServices.Models;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Mappers;
using CTUScheduler.Presentation.Features.Timetable.Models;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using CTUScheduler.Presentation.Features.TimetableRefactor.Resources;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.Timetable.ViewModels;

public abstract class TimetableLayoutBaseViewModel: ViewModelBase, IDisposable
{
    protected readonly CompositeDisposable Disposables = new CompositeDisposable();
    protected readonly CourseColorProvider ColorProvider = new();
    private string _name = "New Schedule";
    private int _subjectCount = 0;
    private int _totalCredit = 0;
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    private TimetableViewModel _visualizerVM;
  
    
    public string Name 
    { 
        get => _name;
        protected set => this.RaiseAndSetIfChanged(ref _name, value); 
    }
    public int SubjectsCount
    {
        get => _subjectCount;
        protected set => this.RaiseAndSetIfChanged(ref _subjectCount, value); 
    }
    public  int TotalCredit 
    { 
        get => _totalCredit;
        protected set => this.RaiseAndSetIfChanged(ref _totalCredit, value);  
    }

    public DateTimeOffset LastUpdated
    {
        get => _lastUpdated;
        protected set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }
    public TimetableViewModel VisualizerVM 
    {
        get => _visualizerVM;
        protected set => this.RaiseAndSetIfChanged(ref _visualizerVM, value);
    }
    
    public ReactiveCommand<object,Unit> ExportToImageCommand { get; protected set; }
    public ReactiveCommand<Unit,Unit> ExportToExcelCommand { get; protected set; }

    public TimetableLayoutBaseViewModel()
    {
        ExportToImageCommand = ReactiveCommand.CreateFromTask<object>(async (view) => { })
            .DisposeWith(Disposables);

        ExportToExcelCommand = ReactiveCommand.CreateFromTask(async () => { })
            .DisposeWith(Disposables);

        VisualizerVM?.DisposeWith(Disposables);
    }

    public void AddToGrid(Course course, CourseSection section)
    {
        var (groupCellShared, cells) = ScheduleUiMapper.ToScheduleCells(course,section);
        var cellList = cells.ToList();
        if (cellList.Count == 0)
        {
            // VisualizerVM.AddUnscheduledSubject(groupCellShared);
        }
        else
        {
            // VisualizerVM.AddCells(groupCellShared, cellList);
        }
        AddCredits(groupCellShared.Credit);
        SubjectsCount++;
    }
    
    protected virtual void AddCredits(int credits) => TotalCredit += credits;
    
    // protected TimetableRenderItem CreateRenderItem(RuntimeCourse runtimeCourse, CourseSection section)
    // {
    //     // 1. Lấy màu (cùng logic cho cả 2 VM)
    //     var color = _colorProvider.GetColorForCourse(runtimeCourse.Code);
    //
    //     // 2. Tạo Shared Object (Reactive)
    //     // var shared = new ScheduleGroupCellShared(runtimeCourse, section, color);
    //
    //     // 3. Tạo Cells UI
    //     // var cells = section.ClassDays.Select(day => new ScheduleCellUi(shared)
    //     // {
    //     //     Room = day.Room,
    //     //     AttendingDay = day.AttendingDay,
    //     //     StartPeriod = day.StartPeriod(),
    //     //     NumberOfPeriods = day.PeriodCount()
    //     // });
    //
    //     // return new TimetableRenderItem(shared, cells);
    // }

    public virtual void Dispose()
    {
        Disposables.Dispose();
    }
}