using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Core.Models.Settings;
using CTUScheduler.Presentation.Base;
using CTUScheduler.Presentation.Features.Grades.Helpers;
using CTUScheduler.Presentation.Services.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SkiaSharp;

namespace CTUScheduler.Presentation.Features.Grades.ViewModels.Components;

public sealed partial class GradeChartsViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly BehaviorSubject<GradeTranscript?> _transcriptSubject = new(null);
    private bool _isDisposed;

    [Reactive] private bool _isDistributionByCredits = true;
    [Reactive] private SemesterFilterOption? _selectedCourseFilter = SemesterFilterOption.All;
    [Reactive] private IReadOnlyList<SemesterFilterOption> _courseFilterOptions = [SemesterFilterOption.All];

    [Reactive] private ISeries[] _gpaSeries = [];
    [Reactive] private Axis[] _gpaXAxes = [];
    [Reactive] private Axis[] _gpaYAxes = [];
    [Reactive] private bool _hasGpaData;

    [Reactive] private ISeries[] _creditSeries = [];
    [Reactive] private Axis[] _creditXAxes = [];
    [Reactive] private Axis[] _creditYAxes = [];
    [Reactive] private bool _hasCreditData;

    [Reactive] private ISeries[] _distributionSeries = [];
    [Reactive] private bool _hasDistributionData;

    [Reactive] private ISeries[] _conductSeries = [];
    [Reactive] private Axis[] _conductXAxes = [];
    [Reactive] private Axis[] _conductYAxes = [];
    [Reactive] private bool _hasConductData;

    [Reactive] private ISeries[] _courseScoreSeries = [];
    [Reactive] private Axis[] _courseScoreXAxes = [];
    [Reactive] private Axis[] _courseScoreYAxes = [];
    [Reactive] private bool _hasCourseScoreData;
    [Reactive] private double _courseChartHeight = 260;

    [Reactive] private SolidColorPaint _legendTextPaint = CreatePaint("#343A40");
    [Reactive] private SolidColorPaint _tooltipTextPaint = CreatePaint("#1F2937");
    [Reactive] private SolidColorPaint _tooltipBackgroundPaint = CreatePaint("#FFFFFF");

    public GradeChartsViewModel(IThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(themeService);
        _transcriptSubject.DisposeWith(_disposables);

        var themeStream = themeService.ThemeChanged
            .StartWith(themeService.CurrentTheme)
            .DistinctUntilChanged()
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        Observable.CombineLatest(
                _transcriptSubject,
                themeStream,
                (transcript, theme) => (transcript, theme))
            .Subscribe(x => RebuildTrendAndThemeCharts(x.transcript, x.theme))
            .DisposeWith(_disposables);

        Observable.CombineLatest(
                _transcriptSubject,
                this.WhenAnyValue(x => x.IsDistributionByCredits),
                themeStream,
                (transcript, byCredits, theme) => (transcript, byCredits, theme))
            .Subscribe(x => RebuildDistributionChart(x.transcript, x.byCredits, x.theme))
            .DisposeWith(_disposables);

        Observable.CombineLatest(
                _transcriptSubject,
                this.WhenAnyValue(x => x.SelectedCourseFilter),
                themeStream,
                (transcript, filter, theme) => (transcript, filter, theme))
            .Subscribe(x => RebuildCourseScoreChart(x.transcript, x.filter, x.theme))
            .DisposeWith(_disposables);
    }

    public bool IsDistributionByCourses
    {
        get => !IsDistributionByCredits;
        set
        {
            if (value)
            {
                IsDistributionByCredits = false;
            }
        }
    }

    public void UpdateTranscript(GradeTranscript? transcript)
    {
        var previousSelection = SelectedCourseFilter;

        if (transcript is null || transcript.Semesters.Count == 0)
        {
            CourseFilterOptions = [SemesterFilterOption.All];
            SelectedCourseFilter = SemesterFilterOption.All;
            _transcriptSubject.OnNext(null);
            return;
        }

        var yearNodes = transcript.Semesters
            .GroupBy(s => s.AcademicYearStart)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                string yearLabel = $"Năm học {g.Key} - {g.Key + 1}";
                var children = g
                    .OrderByDescending(s => s.Term)
                    .Select(s => new SemesterFilterOption(
                        s.AcademicYearStart,
                        s.Term,
                        $"Học kỳ {s.Term}",
                        GradeVisualHelper.FormatSemesterTitle(s.AcademicYearStart, s.Term),
                        []))
                    .ToList();

                return new SemesterFilterOption(g.Key, null, yearLabel, yearLabel, children);
            })
            .ToList();

        var options = new List<SemesterFilterOption>(yearNodes.Count + 1)
        {
            SemesterFilterOption.All
        };
        options.AddRange(yearNodes);
        CourseFilterOptions = options;

        var matching = previousSelection is not null
            ? options
                .SelectMany(o => o.Children.Count > 0 ? o.Children.Prepend(o) : [o])
                .FirstOrDefault(o =>
                    o.AcademicYearStart == previousSelection.AcademicYearStart &&
                    o.Term == previousSelection.Term)
            : null;
        SelectedCourseFilter = matching ?? SemesterFilterOption.All;

        _transcriptSubject.OnNext(transcript);
    }

    private void RebuildTrendAndThemeCharts(GradeTranscript? transcript, AppTheme theme)
    {
        var palette = ChartPalette.ForTheme(theme);
        LegendTextPaint = CreatePaint(palette.LabelHex);
        TooltipTextPaint = CreatePaint(palette.LabelHex);
        TooltipBackgroundPaint = CreatePaint(palette.TooltipBgHex);

        var trends = GradeStatisticsHelper.BuildSemesterTrends(transcript);
        if (trends.Count == 0)
        {
            HasGpaData = false;
            GpaSeries = [];
            GpaXAxes = [];
            GpaYAxes = [];

            HasCreditData = false;
            CreditSeries = [];
            CreditXAxes = [];
            CreditYAxes = [];

            HasConductData = false;
            ConductSeries = [];
            ConductXAxes = [];
            ConductYAxes = [];
            return;
        }

        var labels = trends.Select(t => t.ShortLabel).ToArray();

        // 1. Xu hướng GPA
        HasGpaData = true;
        GpaSeries =
        [
            new ColumnSeries<double>
            {
                Name = "ĐTB học kỳ",
                Values = trends.Select(t => t.SemesterGpa).ToArray(),
                Fill = CreatePaint(palette.PrimaryHex),
                MaxBarWidth = 26,
                YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:0.00}"
            },
            new LineSeries<double>
            {
                Name = "ĐTB tích lũy",
                Values = trends.Select(t => t.CumulativeGpa).ToArray(),
                Stroke = CreatePaint(palette.AccentHex, 3),
                Fill = null,
                GeometrySize = 8,
                GeometryFill = CreatePaint(palette.SurfaceHex),
                GeometryStroke = CreatePaint(palette.AccentHex, 2.5f),
                LineSmoothness = 0.25,
                YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:0.00}"
            }
        ];
        GpaXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                LabelsRotation = labels.Length > 6 ? -20 : 0,
                MinStep = 1,
                ForceStepToMin = true
            }
        ];
        GpaYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 4.2,
                MinStep = 1,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                SeparatorsPaint = CreatePaint(palette.SeparatorHex, 1),
                Labeler = v => v <= 4.0 ? v.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty
            }
        ];

        // 2. Tiến độ tín chỉ (2 trục Y)
        HasCreditData = true;
        CreditSeries =
        [
            new ColumnSeries<int>
            {
                Name = "Tín chỉ kỳ",
                Values = trends.Select(t => t.SemesterAccumulatedCredits).ToArray(),
                Fill = CreatePaint(palette.SecondaryHex),
                MaxBarWidth = 26,
                ScalesYAt = 0,
                YToolTipLabelFormatter = p => $"{(int)p.Coordinate.PrimaryValue} TC"
            },
            new LineSeries<int>
            {
                Name = "Tích lũy cộng dồn",
                Values = trends.Select(t => t.TotalAccumulatedCredits).ToArray(),
                Stroke = CreatePaint(palette.PrimaryHex, 3),
                Fill = null,
                GeometrySize = 8,
                GeometryFill = CreatePaint(palette.SurfaceHex),
                GeometryStroke = CreatePaint(palette.PrimaryHex, 2.5f),
                LineSmoothness = 0.2,
                ScalesYAt = 1,
                YToolTipLabelFormatter = p => $"{(int)p.Coordinate.PrimaryValue} TC"
            }
        ];
        CreditXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                LabelsRotation = labels.Length > 6 ? -20 : 0,
                MinStep = 1,
                ForceStepToMin = true
            }
        ];
        CreditYAxes =
        [
            new Axis
            {
                Name = "TC kỳ",
                NameTextSize = 11,
                NamePaint = CreatePaint(palette.SubTextHex),
                MinLimit = 0,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                SeparatorsPaint = CreatePaint(palette.SeparatorHex, 1),
                Position = AxisPosition.Start
            },
            new Axis
            {
                Name = "Tổng TC",
                NameTextSize = 11,
                NamePaint = CreatePaint(palette.SubTextHex),
                MinLimit = 0,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                SeparatorsPaint = null,
                Position = AxisPosition.End
            }
        ];

        // 3. Điểm rèn luyện (chỉ các kỳ có ĐRL)
        var conductTrends = trends.Where(t => t.ConductScore.HasValue).ToList();
        HasConductData = conductTrends.Count > 0;
        if (conductTrends.Count > 0)
        {
            var conductLabels = conductTrends.Select(t => t.ShortLabel).ToArray();
            ConductSeries =
            [
                new LineSeries<int>
                {
                    Name = "Điểm rèn luyện",
                    Values = conductTrends.Select(t => t.ConductScore!.Value).ToArray(),
                    Stroke = CreatePaint(palette.GreenHex, 3),
                    Fill = new SolidColorPaint(SKColor.Parse(palette.GreenHex).WithAlpha(45)),
                    GeometrySize = 9,
                    GeometryFill = CreatePaint(palette.SurfaceHex),
                    GeometryStroke = CreatePaint(palette.GreenHex, 2.5f),
                    LineSmoothness = 0.25,
                    YToolTipLabelFormatter = p => $"{(int)p.Coordinate.PrimaryValue} đ"
                }
            ];
            ConductXAxes =
            [
                new Axis
                {
                    Labels = conductLabels,
                    LabelsPaint = CreatePaint(palette.SubTextHex),
                    LabelsRotation = conductLabels.Length > 6 ? -20 : 0,
                    MinLimit = -0.5,
                    MaxLimit = conductLabels.Length - 0.5,
                    MinStep = 1,
                    ForceStepToMin = true
                }
            ];
            ConductYAxes =
            [
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 108,
                    MinStep = 20,
                    LabelsPaint = CreatePaint(palette.SubTextHex),
                    SeparatorsPaint = CreatePaint(palette.SeparatorHex, 1),
                    Labeler = v => v <= 100 ? v.ToString("0", CultureInfo.InvariantCulture) : string.Empty
                }
            ];
        }
        else
        {
            ConductSeries = [];
            ConductXAxes = [];
            ConductYAxes = [];
        }
    }

    private void RebuildDistributionChart(GradeTranscript? transcript, bool byCredits, AppTheme theme)
    {
        this.RaisePropertyChanged(nameof(IsDistributionByCourses));
        var palette = ChartPalette.ForTheme(theme);
        var items = GradeStatisticsHelper.BuildLetterDistribution(transcript, byCredits);

        HasDistributionData = items.Count > 0;
        if (items.Count == 0)
        {
            DistributionSeries = [];
            return;
        }

        string unit = byCredits ? "TC" : "môn";
        DistributionSeries = items
            .Select(item => (ISeries)new PieSeries<double>
            {
                Name = $"{item.Label} ({item.Value:0} {unit})",
                Values = [item.Value],
                Fill = CreatePaint(item.ColorHex),
                Stroke = CreatePaint(palette.SurfaceHex, 2),
                InnerRadius = 56,
                HoverPushout = 6,
                DataLabelsPaint = item.Percentage >= 6 ? CreatePaint("#FFFFFF") : null,
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsFormatter = _ => item.Percentage >= 6 ? $"{item.Label} ({item.Percentage:0}%)" : string.Empty,
                ToolTipLabelFormatter = _ => $"{item.Label}: {item.Value:0} {unit} ({item.Percentage:0.0}%)"
            })
            .ToArray();
    }

    private void RebuildCourseScoreChart(
        GradeTranscript? transcript,
        SemesterFilterOption? filter,
        AppTheme theme)
    {
        var palette = ChartPalette.ForTheme(theme);
        var points = GradeStatisticsHelper.BuildCourseScorePoints(
            transcript,
            filter?.AcademicYearStart,
            filter?.Term);

        HasCourseScoreData = points.Count > 0;
        if (points.Count == 0)
        {
            CourseScoreSeries = [];
            CourseScoreXAxes = [];
            CourseScoreYAxes = [];
            CourseChartHeight = 240;
            return;
        }

        // RowSeries vẽ index 0 ở dưới cùng lên trên, đảo ngược để học phần đầu khóa ở trên cùng đi dần xuống kỳ mới nhất.
        var displayPoints = points.Reverse().ToList();
        CourseChartHeight = Math.Max(360, displayPoints.Count * 30);

        CourseScoreSeries =
        [
            new RowSeries<double>
            {
                Name = "Điểm hệ 10",
                Values = displayPoints.Select(p => p.NumericScore).ToArray(),
                Fill = CreatePaint(palette.PrimaryHex),
                MaxBarWidth = 18,
                ShowDataLabels = true,
                DataLabelsSize = 11,
                DataLabelsPaint = CreatePaint(palette.LabelHex),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => p.Index >= 0 && p.Index < displayPoints.Count
                    ? $"{displayPoints[p.Index].NumericScore:0.0}"
                    : $"{p.Coordinate.PrimaryValue:0.0}",
                XToolTipLabelFormatter = p => p.Index >= 0 && p.Index < displayPoints.Count
                    ? $"{displayPoints[p.Index].CourseCode} - {displayPoints[p.Index].CourseName} ({displayPoints[p.Index].SemesterTitle})"
                    : string.Empty,
                YToolTipLabelFormatter = p => p.Index >= 0 && p.Index < displayPoints.Count
                    ? $"{displayPoints[p.Index].NumericScore:0.0} ({displayPoints[p.Index].LetterGrade.ToDisplayLabel()})"
                    : $"{p.Coordinate.PrimaryValue:0.0}"
            }
        ];

        CourseScoreXAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 10.5,
                MinStep = 2,
                LabelsPaint = CreatePaint(palette.SubTextHex),
                SeparatorsPaint = CreatePaint(palette.SeparatorHex, 1)
            }
        ];

        CourseScoreYAxes =
        [
            new Axis
            {
                Labels = displayPoints.Select(p => $"{p.CourseCode} · {Truncate(p.CourseName, 26)}").ToArray(),
                LabelsPaint = CreatePaint(palette.LabelHex),
                MinStep = 1,
                ForceStepToMin = true
            }
        ];
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : $"{text[..(maxLength - 1)]}…";

    private static SolidColorPaint CreatePaint(string hex, float strokeThickness = 0)
    {
        var paint = new SolidColorPaint(SKColor.Parse(hex));
        if (strokeThickness > 0)
        {
            paint.StrokeThickness = strokeThickness;
        }
        return paint;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _disposables.Dispose();
        _isDisposed = true;
    }

    private sealed record ChartPalette(
        string PrimaryHex,
        string SecondaryHex,
        string AccentHex,
        string GreenHex,
        string LabelHex,
        string SubTextHex,
        string SeparatorHex,
        string SurfaceHex,
        string TooltipBgHex)
    {
        public static ChartPalette ForTheme(AppTheme theme) =>
            theme == AppTheme.Dark
                ? new ChartPalette(
                    PrimaryHex: "#3B82F6",
                    SecondaryHex: "#38BDF8",
                    AccentHex: "#F59E0B",
                    GreenHex: "#10B981",
                    LabelHex: "#F3F4F6",
                    SubTextHex: "#9CA3AF",
                    SeparatorHex: "#2D2D2D",
                    SurfaceHex: "#1E1E1E",
                    TooltipBgHex: "#262626")
                : new ChartPalette(
                    PrimaryHex: "#1F5CA9",
                    SecondaryHex: "#00AFEF",
                    AccentHex: "#FE6A35",
                    GreenHex: "#00B574",
                    LabelHex: "#343A40",
                    SubTextHex: "#6B7280",
                    SeparatorHex: "#E5E7EB",
                    SurfaceHex: "#FFFFFF",
                    TooltipBgHex: "#FFFFFF");
    }
}
