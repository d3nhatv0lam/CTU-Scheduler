using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CTUScheduler.Core.Models.Timetable;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace CTUScheduler.Presentation.Features.Scheduling.ViewModels.Components;

/// <summary>
/// Quản lý bộ lọc tránh học ngày/buổi
/// </summary>
public partial class AvoidanceFilterViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public IReadOnlyList<DayAvoidanceItemViewModel> Days { get; }

    [ObservableAsProperty] private bool _hasActiveFilters;
    [ObservableAsProperty] private string _summaryText = "Không áp dụng";

    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }

    public AvoidanceFilterViewModel()
    {
        Days = new List<DayAvoidanceItemViewModel>
        {
            new(DayOfWeek.Monday,    "T2", "Thứ Hai"),
            new(DayOfWeek.Tuesday,   "T3", "Thứ Ba"),
            new(DayOfWeek.Wednesday, "T4", "Thứ Tư"),
            new(DayOfWeek.Thursday,  "T5", "Thứ Năm"),
            new(DayOfWeek.Friday,    "T6", "Thứ Sáu"),
            new(DayOfWeek.Saturday,  "T7", "Thứ Bảy")
        };

        // Quan sát thay đổi từ bất kỳ ngày nào
        var changesObservable = Days
            .Select(d => d.WhenAnyValue(x => x.IsAvoided, x => x.SelectedOption, (avoided, opt) => (avoided, opt)))
            .Merge();

        // HasActiveFilters
        _hasActiveFiltersHelper = changesObservable
            .Select(_ => Days.Any(d => d.IsAvoided))
            .ToProperty(this, nameof(HasActiveFilters), initialValue: false);

        // SummaryText
        _summaryTextHelper = changesObservable
            .Select(_ => BuildSummary())
            .ToProperty(this, nameof(SummaryText), initialValue: "Không áp dụng");

        ClearAllCommand = ReactiveCommand.Create(ClearAll);
    }

    private string BuildSummary()
    {
        var activeDays = Days.Where(d => d.IsAvoided).ToList();
        if (activeDays.Count == 0)
        {
            return "Không áp dụng";
        }

        var parts = activeDays.Select(d =>
        {
            if (d.SelectedOption.Mode == Models.DayAvoidanceMode.AllDay)
            {
                return d.ShortLabel;
            }
            return $"{d.ShortLabel} ({d.SelectedOption.ShortName})";
        });

        return $"Tránh: {string.Join(", ", parts)}";
    }

    /// <summary>
    /// Xóa toàn bộ lựa chọn tránh lịch.
    /// </summary>
    public void ClearAll()
    {
        foreach (var day in Days)
        {
            day.Reset();
        }
    }

    /// <summary>
    /// Lấy danh sách các slot cần tránh để truyền cho IPruningRule.
    /// </summary>
    public IReadOnlyList<AvoidanceSlot> GetActiveAvoidanceSlots()
    {
        return Days
            .Select(d => d.GetAvoidanceSlot())
            .Where(slot => slot != null)
            .Select(slot => slot!)
            .ToList();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
