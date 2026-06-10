using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Features.TimetableRefactor.Resources;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using ReactiveUI;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public abstract class TimetableLayoutBaseViewModel : ViewModelBase, IDisposable
{
    private readonly CourseColorProvider _colorProvider = new();
    protected readonly CompositeDisposable Disposables = new();
    protected readonly IExcelExporterService ExcelExporter;
    protected readonly ITimetablePreviewRenderer TimetablePreviewRenderer;
    public IControlRendererService ControlRendererService { get; }
    protected readonly IUserInteractionService UserInteractionService;
    private string _name = "New Schedule";
    private int _subjectCount = 0;
    private int _totalCredits = 0;
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    private TimetableViewModel? _visualizerVM = null;
    private bool _isSelected;
    private bool _isEnabled = true;
    private Bitmap? _previewImage;


    private bool _isDisposed;

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

    public virtual TimetableViewModel? VisualizerVM
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

    public virtual Bitmap? PreviewImage
    {
        get => _previewImage;
        set
        {
            var oldImage = _previewImage;
            if (oldImage == value) return;
            this.RaiseAndSetIfChanged(ref _previewImage, value);
            if (oldImage is not null)
            {
                Dispatcher.UIThread.Post(oldImage.Dispose, DispatcherPriority.Background);
            }
        }
    }

    public ReactiveCommand<Unit, Unit> CopyToClipboardCommand { get; protected set; }
    public ReactiveCommand<Unit, Unit> ExportToExcelCommand { get; protected set; }

    public Interaction<Unit, bool> CopyToClipboardInteraction { get; } = new();


    public TimetableLayoutBaseViewModel(
        IExcelExporterService excelExporter,
        IControlRendererService controlRendererService,
        ITimetablePreviewRenderer timetablePreviewRenderer,
        IUserInteractionService userInteractionService)
    {
        ExcelExporter = excelExporter;
        ControlRendererService = controlRendererService;
        TimetablePreviewRenderer = timetablePreviewRenderer;
        UserInteractionService = userInteractionService;

        CopyToClipboardCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var success = await CopyToClipboardInteraction.Handle(Unit.Default);
            if (success)
            {
                UserInteractionService.Toast.Light.Success("Thành công",
                    "Đã sao chép ảnh thời khóa biểu vào clipboard!");
            }
            else
            {
                UserInteractionService.Notification.Light.Error("Thất bại", "Không thể sao chép ảnh vào clipboard.");
            }
        }).DisposeWith(Disposables);

        ExportToExcelCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var blueprint = ToScheduleBlueprint();
            if (!blueprint.IsConsistent) return;

            var safeName = string.IsNullOrWhiteSpace(this.Name) ? "TKB" : this.Name.Trim();
            string fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fullPath = Path.Combine(desktopPath, fileName);

            await ExcelExporter.ExportTimetableAsync(blueprint, fullPath);
        }).DisposeWith(Disposables);
    }

    protected async Task GeneratePreviewAsync(CancellationToken cancellationToken)
    {
        if (VisualizerVM is null) return;

        try
        {
            PreviewImage = await TimetablePreviewRenderer.RenderPreviewAsync(VisualizerVM, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    protected TimetableRenderItem CreateRenderItem(ICourseDisplaySource dataSource)
    {
        string code = "";
        using var _ = dataSource.Code.Take(1).Subscribe(c => code = c);
        var color = _colorProvider.GetColorForCourse(code);

        var shared = new ScheduleGroupCellShared(dataSource, color);

        var cells = dataSource.ClassDays.Select(day => new ScheduleCellUi(shared)
        {
            Room = day.Room,
            AttendingDay = day.AttendingDay,
            StartPeriod = day.StartPeriod,
            NumberOfPeriods = day.PeriodCount
        });

        return new TimetableRenderItem(shared, cells);
    }

    public abstract ScheduleBlueprint ToScheduleBlueprint();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (_isDisposed) return;

        if (isDisposing)
        {
            // gọi trực tiếp _previewImage để tránh gọi lazy property tự khởi tạo bitmap sau đó dispose
            if (_previewImage is not null)
            {
                var img = _previewImage;
                _previewImage = null;
                Dispatcher.UIThread.Post(img.Dispose, DispatcherPriority.Background);
            }
            Disposables.Dispose();
        }

        _isDisposed = true;
    }
}