using System.Reactive.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.Benchmarks.Datasets;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
using CTUScheduler.Core.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace CTUScheduler.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TimetableGeneratorBenchmarks
{
    private TimetableGeneratorService _service = null!;
    private ScheduleGenerationOptions _options = null!;
    private Dictionary<int, List<IReadOnlyList<SectionChoice>>> _cachedSets = null!;

    [Params(4, 5, 6, 7, 8)]
    public int CourseCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _service = new TimetableGeneratorService(NullLogger<TimetableGeneratorService>.Instance);
        _options = new ScheduleGenerationOptions
        {
            MaxResults = 1000
        };

        _cachedSets = new Dictionary<int, List<IReadOnlyList<SectionChoice>>>();
        for (int i = 4; i <= 8; i++)
        {
            _cachedSets[i] = CtuRealCourseDataset.GetCourseSets(i);
        }
    }

    [Benchmark(Description = "CTU-Scheduler (With Pruning)")]
    public async Task<int> GenerateWithPruning()
    {
        var sets = _cachedSets[CourseCount];
        var results = await _service.Generate(sets, _options).FirstAsync();
        return results.Count;
    }

    [Benchmark(Baseline = true, Description = "Naive Brute Force (No Pruning)")]
    public int GenerateBruteForce()
    {
        var sets = _cachedSets[CourseCount];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        int count = 0;
        try
        {
            var results = Combinatorics.CartesianProduct(
                sets,
                isValidCandidate: null,
                isValidFull: ValidateNoOverlap,
                token: cts.Token
            );

            foreach (var _ in results)
            {
                count++;
            }
        }
        catch (OperationCanceledException)
        {
            // Ngắt do vượt quá ngưỡng Timeout 10 giây
        }
        return count;
    }

    private static bool ValidateNoOverlap(ReadOnlySpan<SectionChoice> fullTimetable)
    {
        for (int i = 0; i < fullTimetable.Length - 1; i++)
        {
            for (int j = i + 1; j < fullTimetable.Length; j++)
            {
                if (ScheduleValidator.IsOverlapTimeTable(fullTimetable[i].Section, fullTimetable[j].Section))
                    return false;
            }
        }
        return true;
    }
}
