using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.TimetableGeneratorService;

public class TimetableGeneratorService : ITimetableGeneratorService
{
    private readonly ILogger<TimetableGeneratorService> _logger;

    public TimetableGeneratorService(ILogger<TimetableGeneratorService> logger)
    {
        _logger = logger;
    }

    public IObservable<IReadOnlyList<RawTimetableData>> Generate(IEnumerable<IReadOnlyList<SectionChoice>> sets,
        ScheduleGenerationOptions? options = null)
    {
        var opts = options ?? new ScheduleGenerationOptions();
        IPruningRule[] pruningArray = [new OverlapPruningRule(), .. opts.AdditionalPruningRules];
        IPostFilterRule[] postFilterArray = opts.AdditionalPostFilterRules.ToArray();
        IScheduleScorer[] scorers = opts.Scorers.ToArray();

        _logger.LogInformation("Bắt đầu tạo lịch. MaxResults: {Max}, Timeout: {Timeout}",
            opts.MaxResults, opts.Timeout);

        return Observable.Create<IReadOnlyList<RawTimetableData>>(observer =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(opts.CancellationToken);
            if (opts.Timeout.HasValue) linkedCts.CancelAfter(opts.Timeout.Value);
            var token = linkedCts.Token;

            var queue = new PriorityQueue<RawTimetableData, double>();
            int count = 0;
            int? max = opts.MaxResults;
            try
            {
                var orderedSetList = sets
                    .Where(s => s is not null && s.Count > 0)
                    .OrderBy(s => s.Count)
                    .ToList();

                _logger.LogDebug("Dữ liệu đầu vào: {Count} nhóm môn học.", orderedSetList.Count);

                var results = Combinatorics.CartesianProduct(
                    orderedSetList,
                    isValidCandidate: (path, candidate) => ValidateCandidate(path, candidate, pruningArray),
                    isValidFull: fullTimetable => ValidateFull(fullTimetable, postFilterArray),
                    token
                );

                foreach (var schedule in results)
                {
                    count++;
                    var rawTimetable = new RawTimetableData(schedule)
                    {
                        Score = CalculateScore(schedule, scorers)
                    };

                    if (max.HasValue && queue.Count >= max)
                    {
                        if (queue.TryPeek(out _, out var priority)
                            && priority > rawTimetable.Score) continue;

                        queue.Dequeue();
                    }

                    queue.Enqueue(rawTimetable, rawTimetable.Score);
                }

                if (!token.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Hoàn tất tạo lịch. Tổng số kết quả tìm thấy: {Count}, giữ lại {QueueCount} kết quả tốt nhất",
                        count,
                        queue.Count);

                    observer.OnNext(queue.ToReverseOrderList());
                    observer.OnCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Tiến trình tạo lịch bị hủy bỏ (Timeout hoặc yêu cầu từ người dùng). Đưa ra {Count} kết quả đã tìm thấy",
                    queue.Count);
                observer.OnNext(queue.ToReverseOrderList());
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong quá trình tạo lịch.");
                observer.OnError(ex);
            }

            return Disposable.Empty;
        });
    }

    public double RecalculateScore(IReadOnlyList<SectionChoice> schedule, IReadOnlyList<IScheduleScorer> scorers)
    {
        return CalculateScore(schedule, scorers.ToArray());
    }
    
    private static bool ValidateCandidate(
        ReadOnlySpan<SectionChoice> path,
        SectionChoice candidate,
        IPruningRule[] rules)
    {
        for (int i = 0; i < rules.Length; i++)
        {
            if (!rules[i].CanContinue(path, candidate)) return false;
        }

        return true;
    }

    private static bool ValidateFull(
        ReadOnlySpan<SectionChoice> fullTimetable,
        IPostFilterRule[] filters)
    {
        for (int i = 0; i < filters.Length; i++)
        {
            if (!filters[i].IsSatisfied(fullTimetable)) return false;
        }

        return true;
    }

    private static double CalculateScore(IReadOnlyList<SectionChoice> schedule, IScheduleScorer[] scorers)
    {
        var totalScore = 0D;
        var totalWeight = 0D;

        foreach (var score in scorers)
        {
            totalWeight += score.Weight;
            totalScore += score.CalculateScore(schedule) * score.Weight;
        }

        return totalWeight > 0 ? Math.Clamp(totalScore / totalWeight, 0D, 1D) : 0D;
    }
}