using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Timetable.Resources;
using CTUScheduler.Presentation.Features.Timetable.ViewModels;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Features.TimetableRefactor.Resources;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public abstract class TimetableLayoutBaseViewModel: ViewModelBase, IDisposable
{
    protected readonly CompositeDisposable Disposables = new();
    private readonly CourseColorProvider _colorProvider = new();    
    private string _name = "New Schedule";
    private int _subjectCount = 0;
    private int _totalCredits = 0;
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
    public int TotalCredits 
    { 
        get => _totalCredits;
        protected set => this.RaiseAndSetIfChanged(ref _totalCredits, value);  
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
    

    protected TimetableRenderItem CreateRenderItem(ICourseDisplaySource dataSource)
    {
        string code = "";
        dataSource.Code.Take(1).Subscribe(c => code = c); 
        var color = _colorProvider.GetColorForCourse(code);
        
        var shared = new ScheduleGroupCellShared(dataSource, color);
        
        var cells = dataSource.ClassDays.Select(day => new ScheduleCellUi(shared)
        {
            Room = day.Room,
            AttendingDay = day.AttendingDay,
            StartPeriod = day.StartPeriod(),
            NumberOfPeriods = day.PeriodCount()
        });

        return new TimetableRenderItem(shared, cells);
    }

    public virtual void Dispose() => Disposables.Dispose();
}