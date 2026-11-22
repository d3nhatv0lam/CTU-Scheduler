using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Curriculum.CourseData;
using CTUScheduler.Core.Models.Academic.Curriculum.Schedule;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Excel;
using CTUScheduler.Presentation.Features.TimetableRefactor.Adapters;
using CTUScheduler.Presentation.Features.TimetableRefactor.Models;
using CTUScheduler.Presentation.Services.ControlRenderer;
using CTUScheduler.Presentation.Services.Theme;
using CTUScheduler.Presentation.Services.UserInteractionService.Interfaces;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.TimetableRefactor.ViewModels;

public partial class TimetablePreviewViewModel : TimetableLayoutBaseViewModel,
    INeedArgs<IEnumerable<SectionChoice>>,
    IScorable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly List<SectionChoice> _choices = [];
    private readonly HashSet<AppTheme> _requestedThemes = [];
    public IReadOnlyList<SectionChoice> Choices => _choices;

    [Reactive] private double _totalScore;
    private AppTheme? _currentImageTheme;

    double IScorable.Score => TotalScore;

    public override Bitmap? PreviewImage
    {
        get
        {
            var currentTheme = ThemeService.CurrentTheme;
            if (TryGetCachedPreviewBytes(currentTheme, out var bytes))
            {
                if (_currentImageTheme != currentTheme || base.PreviewImage is null)
                {
                    var oldImage = base.PreviewImage;
                    using var stream = new MemoryStream(bytes);
                    base.PreviewImage = new Bitmap(stream);
                    _currentImageTheme = currentTheme;

                    if (oldImage is not null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            try { oldImage.Dispose(); }
                            catch (ObjectDisposedException) { }
                        }, DispatcherPriority.Background);
                    }
                }
                return base.PreviewImage;
            }

            EnsurePreviewGeneratedForTheme(currentTheme);
            
            return base.PreviewImage;
        }
        set
        {
            _currentImageTheme = ThemeService.CurrentTheme;
            base.PreviewImage = value;
        }
    }

    private readonly Lock _visualizerLock = new();
    private readonly SerialDisposable _visualizerDisposable = new();

    public override TimetableViewModel? VisualizerVM
    {
        get
        {
            if (base.VisualizerVM is not null || _choices.Count <= 0) return base.VisualizerVM;

            lock (_visualizerLock)
            {
                if (base.VisualizerVM is not null) return base.VisualizerVM;

                var sourceList = new SourceList<TimetableRenderItem>();

                var items = _choices.Select(choice =>
                {
                    var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
                    return CreateRenderItem(adapter);
                }).ToList();

                sourceList.AddRange(items);

                var vm = new TimetableViewModel(sourceList);

                _visualizerDisposable.Disposable = Disposable.Create(() =>
                {
                    foreach (var item in sourceList.Items)
                    {
                        item.Dispose();
                    }

                    sourceList.Dispose();
                    vm.Dispose();
                });

                base.VisualizerVM = vm;
            }

            return base.VisualizerVM;
        }
        protected set
        {
            if (value is null)
            {
                _visualizerDisposable.Disposable = Disposable.Empty;
            }
            base.VisualizerVM = value;
        }
    }

    protected override async Task<Bitmap?> GeneratePreviewAsync(CancellationToken cancellationToken)
    {
        if (_choices.Count == 0) return null;

        using var tempSourceList = new SourceList<TimetableRenderItem>();
        var items = _choices.Select(choice =>
        {
            var adapter = new StaticCourseAdapter(choice.Course, choice.Section);
            return CreateRenderItem(adapter);
        }).ToList();
        tempSourceList.AddRange(items);

        using var tempVm = new TimetableViewModel(tempSourceList);

        try
        {
            return await TimetablePreviewRenderer.RenderPreviewAsync(tempVm, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public TimetablePreviewViewModel(
        IEnumerable<SectionChoice> choices,
        IExcelExporterService excelExporter,
        IControlRendererService controlRendererService,
        ITimetablePreviewRenderer timetablePreviewRenderer,
        IUserInteractionService userInteractionService,
        IThemeService themeService)
        : base(excelExporter, controlRendererService, timetablePreviewRenderer, userInteractionService, themeService)
    {
        if (choices is null)
        {
            SubjectsCount = 0;
            TotalCredits = 0;
            return;
        }

        _choices.AddRange(choices);

        SubjectsCount = _choices.Count;
        TotalCredits = _choices.Sum(x => x.Course.Credits);

        var fullSections = _choices
            .Where(x => x.Section.RemainingStudents <= 0)
            .ToList();

        if (fullSections.Count > 0)
        {
            HasFullSections = true;
            var details = fullSections.Select(x => $"• {x.Course.Code} - {x.Course.Name_VN} (Nhóm {x.Section.Group}: {x.Section.RemainingStudents}/{x.Section.TotalStudents})");
            FullSectionsWarningText = $"Có {fullSections.Count} lớp học phần đã hết slot:\n{string.Join("\n", details)}";
        }
    }

    private void EnsurePreviewGeneratedForTheme(AppTheme theme)
    {
        if (!_requestedThemes.Add(theme)) return;
        try
        {
            var token = _cts.Token;

            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (token.IsCancellationRequested) return;
                await GenerateAndApplyPreviewAsync(token);
            }, DispatcherPriority.Background);
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
    }

    public override ScheduleBlueprint ToScheduleBlueprint()
    {
        int count = _choices.Count;
        var courses = new List<Course>(count);
        var groupKeys = new Dictionary<string, string>(count);
        foreach (var choice in _choices)
        {
            courses.Add(choice.Course.WithSection(choice.Section));
            var courseCode = choice.Course.Code;
            groupKeys.TryAdd(courseCode, choice.Section.Group);
        }

        var profile = new ScheduleProfile()
        {
            Name = this.Name,
            SavedCourseGroupKeys = groupKeys,
            LastUpdated = this.LastUpdated
        };
        return new ScheduleBlueprint(courses, profile);
    }

    protected override void OnThemeChanged(AppTheme newTheme)
    {
        base.OnThemeChanged(newTheme);
        this.RaisePropertyChanged(nameof(PreviewImage));
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }

            _visualizerDisposable.Dispose();
        }

        base.Dispose(isDisposing);
    }
}