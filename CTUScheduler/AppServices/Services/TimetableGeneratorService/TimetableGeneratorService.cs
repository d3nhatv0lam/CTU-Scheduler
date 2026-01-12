using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Algorithms;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Timetable;

namespace CTUScheduler.AppServices.Services.TimetableGeneratorService;

public class TimetableGeneratorService: ITimetableGeneratorService
{
    public IObservable<List<SectionChoice>> Generate(IEnumerable<IReadOnlyList<SectionChoice>> sets, ScheduleGenerationOptions? options)
    {
        var opts = options ?? new ScheduleGenerationOptions();
        return Observable.Create<List<SectionChoice>>(observer =>
        {
            
            var cts = CancellationTokenSource.CreateLinkedTokenSource(opts.CancellationToken);
            if (opts.Timeout.HasValue) cts.CancelAfter(opts.Timeout.Value);

            Task.Run(() =>
            {
                try
                {
                    int count = 0;
                    var results = Combinatorics.CartesianProduct(
                        sets,
                        (path, candidate) => opts.PruningRules.All(r => r.CanContinue(path, candidate)),
                        fullTimetable => opts.PostFilterRules.All(filter => filter.IsSatisfied(fullTimetable)),
                        cts.Token
                    );

                    foreach (var schedule in results)
                    {
                        if (cts.Token.IsCancellationRequested) break;
                        observer.OnNext(schedule);
                        count++;
                        if (opts.MaxResults.HasValue && count >= opts.MaxResults) break;
                    }
                    observer.OnCompleted();
                }
                catch (OperationCanceledException) 
                { 
                    observer.OnCompleted(); 
                }
                catch (Exception ex) { observer.OnError(ex); }
                finally
                {
                    cts.Dispose(); 
                }
            }, cts.Token);
            
            return Disposable.Create(cts, CancelSafe);

            static void CancelSafe(CancellationTokenSource cts)
            {
                try
                {
                    cts.Cancel();
                }
                catch(ObjectDisposedException)
                {
                    // ignored
                }
            }
        });
    }
}