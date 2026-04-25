using System;
using System.Reactive.Linq;

namespace CTUScheduler.AppServices.Extensions;

public static class RxExtensions
{
    public static IObservable<(T? OldValue, T? NewValue)> PairWithPrevious<T>(this IObservable<T> source)
    {
        return source.Scan(
            (OldValue: default(T), NewValue: default(T)),
            (acc, current) => (acc.NewValue, current)
        );
    }
}