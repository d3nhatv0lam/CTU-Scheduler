using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.AppServices.Services.TimetableGeneratorService;

public class TimetableGeneratorService : ITimetableGeneratorService
{
    private const int Batch_Size = (1 << 9) - 1;
    public IObservable<List<SectionChoice>> Generate(IEnumerable<IReadOnlyList<SectionChoice>> sets,
        ScheduleGenerationOptions? options)
    {
        var opts = options ?? new ScheduleGenerationOptions();
        return Observable.Create<List<SectionChoice>>(observer =>
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(opts.CancellationToken);
            if (opts.Timeout.HasValue) linkedCts.CancelAfter(opts.Timeout.Value);

            var token = linkedCts.Token;
            try
            {
                var results = Combinatorics.CartesianProduct(
                    sets,
                    (path, candidate) => opts.PruningRules.All(r => r.CanContinue(path, candidate)),
                    fullTimetable => opts.PostFilterRules.All(filter => filter.IsSatisfied(fullTimetable)),
                    token
                );

                int count = 0;
                int? maxResults = opts.MaxResults;
                foreach (var schedule in results)
                {
                    if ((count & Batch_Size) == 0)
                    {
                        if (token.IsCancellationRequested) break;
                    }
                    observer.OnNext(schedule);
                    count++;
                    if (maxResults.HasValue && count >= maxResults) break;
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

            return Disposable.Create(linkedCts, cts =>
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    /* ignore */
                }
            });
        });
    }
}