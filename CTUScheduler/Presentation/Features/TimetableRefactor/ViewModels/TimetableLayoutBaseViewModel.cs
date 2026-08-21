using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using Avalonia.Xaml.Interactions.DragAndDrop.Controls;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.TimetableRefactor.Interfaces;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Features.TimetableRefactor.Resources;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Services.Theme;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public abstract partial class TimetableLayoutBaseViewModel : ViewModelBase, IDisposable
{
    private readonly CourseColorProvider _colorProvider = new();
    private readonly Dictionary<AppTheme, Bitmap> _previewImagesCached  = new();
    protected readonly CompositeDisposable Disposables = new();
    protected readonly IExcelExporterService ExcelExporter;
    protected readonly ITimetablePreviewRenderer TimetablePreviewRenderer;
    protected readonly IUserInteractionService UserInteractionService;
    protected readonly IThemeService ThemeService;
    private string _name = "New Schedule";
    private int _subjectCount = 0;
    private int _totalCredits = 0;
    private DateTimeOffset _lastUpdated = DateTimeOffset.Now;
    private TimetableViewModel? _visualizerVM = null;
    private bool _isSelected;
    private Bitmap? _previewImage;

    [ObservableAsProperty] private bool _isEditing;

    private bool _isDisposed;

    public IControlRendererService ControlRendererService { get; }

    public string Name
    {
        get => _name;
        protected set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string TempName
    {
        get => field ?? _name;
        set => this.RaiseAndSetIfChanged(ref field, value);
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

    public virtual Bitmap? PreviewImage
    {
        get => _previewImage;
        set => this.RaiseAndSetIfChanged(ref _previewImage, value);
    }

    public ReactiveCommand<Unit, Unit> CopyToClipboardCommand { get; protected set; }
    public ReactiveCommand<Unit, Unit> ExportToExcelCommand { get; protected set; }

    public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public Interaction<Unit, bool> CopyToClipboardInteraction { get; } = new();


    public TimetableLayoutBaseViewModel(
        IExcelExporterService excelExporter,
        IControlRendererService controlRendererService,
        ITimetablePreviewRenderer timetablePreviewRenderer,
        IUserInteractionService userInteractionService,
        IThemeService themeService)
    {
        ExcelExporter = excelExporter;
        ControlRendererService = controlRendererService;
        TimetablePreviewRenderer = timetablePreviewRenderer;
        UserInteractionService = userInteractionService;
        ThemeService = themeService;

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

        StartEditCommand = ReactiveCommand.Create(() => { })
            .DisposeWith(Disposables);

        SaveCommand = ReactiveCommand.Create(() =>
        {
            Name = TempName;
            LastUpdated = DateTimeOffset.Now;
            UserInteractionService.Toast.Light.Success("Cập nhật thông tin thơi khóa biểu thành công!");
        }).DisposeWith(Disposables);

        CancelCommand = ReactiveCommand.Create(() => { TempName = Name; })
            .DisposeWith(Disposables);

        _isEditingHelper = Observable.Merge(
                StartEditCommand.Select(_ => true),
                SaveCommand.Select(_ => false),
                CancelCommand.Select(_ => false)
            )
            .ToProperty(this, nameof(IsEditing), initialValue: false, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(Disposables);
        
        // theme
        ThemeService.ThemeChanged
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(OnThemeChanged)
            .DisposeWith(Disposables);
    }
    
    protected async Task<Bitmap?> GeneratePreviewAsync(CancellationToken cancellationToken)
    {
        if (VisualizerVM is null) return null;
        try
        {
            return await TimetablePreviewRenderer.RenderPreviewAsync(VisualizerVM, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    protected async Task GenerateAndApplyPreviewAsync(CancellationToken cancellationToken)
    {
        var targetTheme = ThemeService.CurrentTheme;
        var bitmap = await GeneratePreviewAsync(cancellationToken);
        
        if (bitmap is not null && !cancellationToken.IsCancellationRequested)
        {
            if (_previewImagesCached.TryGetValue(targetTheme, out var oldImage))
            {
                oldImage.Dispose();
            }
            _previewImagesCached[targetTheme] = bitmap;
            if (ThemeService.CurrentTheme == targetTheme)
            {
                PreviewImage = bitmap;
            }
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

    protected virtual void OnThemeChanged(AppTheme newTheme)
    {
        if (_previewImagesCached.TryGetValue(newTheme, out var image))
        {
            PreviewImage = image;
        }
    }

    protected void DisposePreviewImages()
    {
        if (_previewImagesCached.Count == 0 && _previewImage is null) return;

        _previewImage = null;
        try
        {
            foreach (var (_, image) in _previewImagesCached)
            {
                image?.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // ignore   
        }
        finally
        {
            _previewImagesCached.Clear();
        }
    }

    protected bool HasCachedPreview(AppTheme theme)
    {
        return _previewImagesCached.ContainsKey(theme);
    }
    
    protected bool TryGetCachedPreview(AppTheme theme, [NotNullWhen(true)] out Bitmap? bitmap) 
        => _previewImagesCached.TryGetValue(theme, out bitmap);
    
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
            DisposePreviewImages();
            Disposables.Dispose();
        }

        _isDisposed = true;
    }
}