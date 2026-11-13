using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
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

    double IScorable.Score => TotalScore;

    public override Bitmap? PreviewImage
    {
        get
        {
            var currentTheme = ThemeService.CurrentTheme;
            if (TryGetCachedPreview(currentTheme, out var cachedBmp))
            {
                return cachedBmp;
            }

            EnsurePreviewGeneratedForTheme(currentTheme);
            
            return base.PreviewImage;
        }
        set => base.PreviewImage = value;
    }

    private readonly Lock _visualizerLock = new();

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

                Disposable.Create(sourceList, list =>
                {
                    foreach (var item in list.Items)
                    {
                        item.Dispose();
                    }

                    list.Dispose();
                }).DisposeWith(Disposables);

                var vm = new TimetableViewModel(sourceList)
                    .DisposeWith(Disposables);

                base.VisualizerVM = vm;
            }

            return base.VisualizerVM;
        }
        protected set => base.VisualizerVM = value;
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
        }

        base.Dispose(isDisposing);
    }
}