using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CTUScheduler.AppServices.Services.TimetableGeneratorService;
using CTUScheduler.Benchmarks.Datasets;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;
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
            MaxResults = 500 // Giới hạn lưu trữ Top 500 phương án tối ưu nhất
        };

        _cachedSets = new Dictionary<int, List<IReadOnlyList<SectionChoice>>>();
        for (int i = 4; i <= 8; i++)
        {
            _cachedSets[i] = CtuCourseMockDataset.GetCourseSets(i);
        }
    }

    [Benchmark(Description = "CTU-Scheduler Solve")]
    public async Task<int> GenerateTimetables()
    {
        var sets = _cachedSets[CourseCount];
        var results = await _service.Generate(sets, _options).FirstAsync();
        return results.Count;
    }
}
