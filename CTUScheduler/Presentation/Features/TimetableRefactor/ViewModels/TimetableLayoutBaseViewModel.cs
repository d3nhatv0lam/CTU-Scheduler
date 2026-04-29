using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Features.TimetableRefactor.Resources;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public abstract class TimetableLayoutBaseViewModel : ViewModelBase, IDisposable
{
    private readonly CourseColorProvider _colorProvider = new();
    protected readonly CompositeDisposable Disposables = new();
    protected readonly IExcelExporterService ExcelExporter;
    private string _name = "New Schedule";
    private int _subjectCount = 0;
    private int _totalCredits = 0;
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    private TimetableViewModel _visualizerVM;
    private bool _isSelected;
    private bool _isEnabled = true;

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

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public ReactiveCommand<object, Unit> ExportToImageCommand { get; protected set; }
    public ReactiveCommand<Unit, Unit> ExportToExcelCommand { get; protected set; }


    public TimetableLayoutBaseViewModel(IExcelExporterService excelExporter)
    {
        ExcelExporter = excelExporter ?? throw new ArgumentNullException(nameof(excelExporter));

        ExportToImageCommand = ReactiveCommand.CreateFromTask<object>(async (view) =>
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: Lệnh rỗng");
        }).DisposeWith(Disposables);

        ExportToExcelCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var blueprint = ToScheduleBlueprint();
            if (!blueprint.IsConsistent) return;

            var safeName = string.IsNullOrWhiteSpace(this.Name) ? "TKB" : this.Name.Trim();
            string fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fullPath = System.IO.Path.Combine(desktopPath, fileName);

            await ExcelExporter.ExportTimetableAsync(blueprint, fullPath);
        }).DisposeWith(Disposables);
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

    public abstract ScheduleBlueprint ToScheduleBlueprint();

    public virtual void Dispose() => Disposables.Dispose();
}