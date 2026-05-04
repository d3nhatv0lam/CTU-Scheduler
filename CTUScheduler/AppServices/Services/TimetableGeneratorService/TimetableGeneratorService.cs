using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.AppServices.Services.TimetableGeneratorService;

public class TimetableGeneratorService : ITimetableGeneratorService
{
    public IObservable<IReadOnlyList<SectionChoice>> Generate(IEnumerable<IReadOnlyList<SectionChoice>> sets,
        ScheduleGenerationOptions? options = null)
    {
        var opts = options ?? new ScheduleGenerationOptions();

        IReadOnlyList<IPruningRule> pruningRules =
            [new NoOverlapPruningRule(), .. opts.AdditionalPruningRules];
        IEnumerable<IPostFilterRule> postFilterRules = opts.AdditionalPostFilterRules;

        return Observable.Create<IReadOnlyList<SectionChoice>>(observer =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(opts.CancellationToken);
            if (opts.Timeout.HasValue) linkedCts.CancelAfter(opts.Timeout.Value);

            var token = linkedCts.Token;
            try
            {
                var results = Combinatorics.CartesianProduct(
                    sets,
                    (path, candidate) => pruningRules.All(r => r.CanContinue(path, candidate)),
                    fullTimetable => postFilterRules.All(filter => filter.IsSatisfied(fullTimetable)),
                    token
                );

                int count = 0;
                int? max = opts.MaxResults;
                foreach (var schedule in results)
                {
                    observer.OnNext(schedule);
                    count++;
                    if (max.HasValue && count >= max.Value)
                        break;
                }

                if (!token.IsCancellationRequested)
                    observer.OnCompleted();
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }

            return Disposable.Empty;
        });
    }
}