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

        IPruningRule[] pruningArray = [new OverlapPruningRule(), .. opts.AdditionalPruningRules];
        IPostFilterRule[] postFilterArray = opts.AdditionalPostFilterRules.ToArray();

        return Observable.Create<IReadOnlyList<SectionChoice>>(observer =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(opts.CancellationToken);
            if (opts.Timeout.HasValue) linkedCts.CancelAfter(opts.Timeout.Value);

            var token = linkedCts.Token;
            try
            {
                var results = Combinatorics.CartesianProduct(
                    sets,
                    PruningDelegate,
                    PostFilterDelegate,
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
            
            bool PruningDelegate(IReadOnlyList<SectionChoice> path, SectionChoice candidate)
            {
                foreach (var rule in pruningArray)
                {
                    if (!rule.CanContinue(path, candidate)) return false;
                }

                return true;
            }

            bool PostFilterDelegate(IReadOnlyList<SectionChoice> fullTimetable)
            {
                foreach (var filter in postFilterArray)
                {
                    if (!filter.IsSatisfied(fullTimetable)) return false;
                }

                return true;
            }
        });
    }
}