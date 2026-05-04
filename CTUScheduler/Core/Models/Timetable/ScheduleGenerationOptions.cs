using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CTUScheduler.Core.Interfaces;

namespace CTUScheduler.Core.Models.Timetable;

public record ScheduleGenerationOptions
{
    // --- Cấu hình quản lý tài nguyên ---
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
    public int? MaxResults { get; init; } = 1000; 
    public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(30);

    // --- Cấu hình thuật toán (Strategy Pattern) ---
    public IReadOnlyList<IPruningRule> AdditionalPruningRules { get; init; } = [];
    public IReadOnlyList<IPostFilterRule> AdditionalPostFilterRules { get; init; } = [];
}