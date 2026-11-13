using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CTUScheduler.Core.Models.Timetable;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

/// <summary>
/// Quản lý bộ lọc tránh học ngày / buổi dạng ma trận 7x3 (Thứ 2 -> CN x Sáng/Chiều/Tối).
/// </summary>
public partial class AvoidanceFilterViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Subject<Unit> _filterCommittedSubject = new();
    private string _snapshotSlotsOnOpen = string.Empty;

    public IReadOnlyList<AvoidanceCellViewModel> Cells { get; }

    [ObservableAsProperty] private bool _hasActiveFilters;
    [ObservableAsProperty] private string _summaryText = "Không áp dụng";
    [ObservableAsProperty] private bool _isWeekendPresetActive;
    [ObservableAsProperty] private bool _isEveningPresetActive;
    [ObservableAsProperty] private int _activeSlotsCount;

    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleWeekendPresetCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleEveningPresetCommand { get; }
    public ReactiveCommand<DayOfWeek, Unit> ToggleDayCommand { get; }
    public ReactiveCommand<TimeOfDay, Unit> ToggleTimeCommand { get; }

    /// <summary>
    /// Bắn sự kiện khi Flyout đóng lại và có sự thay đổi thực sự trong bộ lọc.
    /// </summary>
    public IObservable<Unit> FilterCommitted => _filterCommittedSubject.AsObservable();

    public AvoidanceFilterViewModel()
    {
        var days = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };
        var times = new[] { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening };

        // Thứ tự hàng: Hàng 0 (Sáng T2..CN), Hàng 1 (Chiều T2..CN), Hàng 2 (Tối T2..CN)
        var cells = new List<AvoidanceCellViewModel>(21);
        foreach (var time in times)
        {
            foreach (var day in days)
            {
                cells.Add(new AvoidanceCellViewModel(day, time));
            }
        }

        Cells = cells;

        // Quan sát thay đổi từ bất kỳ ô nào
        var anyCellChanged = Cells
            .Select(c => c.WhenAnyValue(x => x.IsAvoided))
            .Merge()
            .Publish()
            .RefCount();

        _activeSlotsCountHelper = anyCellChanged
            .Select(_ => Cells.Count(c => c.IsAvoided))
            .ToProperty(this, nameof(ActiveSlotsCount), initialValue: 0)
            .DisposeWith(_disposables);

        _hasActiveFiltersHelper = anyCellChanged
            .Select(_ => Cells.Any(c => c.IsAvoided))
            .ToProperty(this, nameof(HasActiveFilters), initialValue: false)
            .DisposeWith(_disposables);

        _summaryTextHelper = anyCellChanged
            .Select(_ => BuildSummary())
            .ToProperty(this, nameof(SummaryText), initialValue: "Không áp dụng")
            .DisposeWith(_disposables);

        // Trạng thái preset cuối tuần (đủ 6 ô T7 & CN)
        _isWeekendPresetActiveHelper = anyCellChanged
            .Select(_ => Cells.Where(c => c.Day is DayOfWeek.Saturday or DayOfWeek.Sunday).All(c => c.IsAvoided))
            .ToProperty(this, nameof(IsWeekendPresetActive), initialValue: false)
            .DisposeWith(_disposables);

        // Trạng thái preset buổi tối (đủ 7 ô Tối)
        _isEveningPresetActiveHelper = anyCellChanged
            .Select(_ => Cells.Where(c => c.Time == TimeOfDay.Evening).All(c => c.IsAvoided))
            .ToProperty(this, nameof(IsEveningPresetActive), initialValue: false)
            .DisposeWith(_disposables);

        ClearAllCommand = ReactiveCommand.Create(ClearAll).DisposeWith(_disposables);
        ToggleWeekendPresetCommand = ReactiveCommand.Create(ToggleWeekendPreset).DisposeWith(_disposables);
        ToggleEveningPresetCommand = ReactiveCommand.Create(ToggleEveningPreset).DisposeWith(_disposables);
        ToggleDayCommand = ReactiveCommand.Create<DayOfWeek>(ToggleDay).DisposeWith(_disposables);
        ToggleTimeCommand = ReactiveCommand.Create<TimeOfDay>(ToggleTimeOfDay).DisposeWith(_disposables);
    }

    public void NotifyFlyoutOpened()
    {
        _snapshotSlotsOnOpen = GetSlotsSignature();
    }

    public void NotifyFlyoutClosed()
    {
        var currentSignature = GetSlotsSignature();
        if (_snapshotSlotsOnOpen != currentSignature)
        {
            _filterCommittedSubject.OnNext(Unit.Default);
        }
    }

    private string GetSlotsSignature()
    {
        return string.Join(";", Cells.Where(c => c.IsAvoided).Select(c => $"{(int)c.Day}:{(int)c.Time}"));
    }

    public void ClearAll()
    {
        foreach (var cell in Cells)
        {
            cell.IsAvoided = false;
        }
    }

    public void ToggleWeekendPreset()
    {
        var weekendCells = Cells.Where(c => c.Day is DayOfWeek.Saturday or DayOfWeek.Sunday).ToList();
        var shouldEnable = !weekendCells.All(c => c.IsAvoided);
        foreach (var cell in weekendCells)
        {
            cell.IsAvoided = shouldEnable;
        }
    }

    public void ToggleEveningPreset()
    {
        var eveningCells = Cells.Where(c => c.Time == TimeOfDay.Evening).ToList();
        var shouldEnable = !eveningCells.All(c => c.IsAvoided);
        foreach (var cell in eveningCells)
        {
            cell.IsAvoided = shouldEnable;
        }
    }

    public void ToggleDay(DayOfWeek day)
    {
        var dayCells = Cells.Where(c => c.Day == day).ToList();
        var shouldEnable = !dayCells.All(c => c.IsAvoided);
        foreach (var cell in dayCells)
        {
            cell.IsAvoided = shouldEnable;
        }
    }

    public void ToggleTimeOfDay(TimeOfDay time)
    {
        var timeCells = Cells.Where(c => c.Time == time).ToList();
        var shouldEnable = !timeCells.All(c => c.IsAvoided);
        foreach (var cell in timeCells)
        {
            cell.IsAvoided = shouldEnable;
        }
    }

    public IReadOnlyList<AvoidanceSlot> GetActiveAvoidanceSlots()
    {
        return Cells
            .Where(c => c.IsAvoided)
            .Select(c => new AvoidanceSlot(c.Day, c.Time))
            .ToList();
    }

    private string BuildSummary()
    {
        var activeSlots = GetActiveAvoidanceSlots();
        if (activeSlots.Count == 0) return "Không áp dụng";

        var daysOrder = new[]
        {
            (DayOfWeek.Monday, "T2"),
            (DayOfWeek.Tuesday, "T3"),
            (DayOfWeek.Wednesday, "T4"),
            (DayOfWeek.Thursday, "T5"),
            (DayOfWeek.Friday, "T6"),
            (DayOfWeek.Saturday, "T7"),
            (DayOfWeek.Sunday, "CN")
        };

        if (activeSlots.Count(s => s.Day is DayOfWeek.Saturday or DayOfWeek.Sunday) == 6 && activeSlots.Count == 6)
        {
            return "Tránh: Cuối tuần (T7, CN)";
        }

        List<string> parts = [];
        foreach (var (day, label) in daysOrder)
        {
            var daySlots = activeSlots.Where(s => s.Day == day && s.Time.HasValue).Select(s => s.Time!.Value).ToList();
            if (daySlots.Count == 0) continue;

            if (daySlots.Count == 3)
            {
                parts.Add($"{label} (Cả ngày)");
            }
            else
            {
                var times = daySlots.Select(t => t switch
                {
                    TimeOfDay.Morning => "Sáng",
                    TimeOfDay.Afternoon => "Chiều",
                    TimeOfDay.Evening => "Tối",
                    _ => ""
                });
                parts.Add($"{label} ({string.Join("/", times)})");
            }
        }

        return $"Tránh: {string.Join(", ", parts)}";
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _filterCommittedSubject.Dispose();
    }
}