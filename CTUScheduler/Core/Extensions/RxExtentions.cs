using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions;

public static class RxExtensions
{
    /// <summary>
    /// AutoSave với Policy Selectorchọn điều kiện bật/tắt từ một object Settings chung.
    /// </summary>
    public static IDisposable AutoSave<TData, TSettings>(
        this IObservable<TData> source,           
        IObservable<TSettings> settingsStream,    
        Func<TSettings, bool> toggleSelector,     
        Func<TData, Task> saveAsync,              
        Action<Exception>? onError = null, 
        IScheduler? scheduler = null,
        TimeSpan? throttle = null) 
        where TData : notnull
        where TSettings : notnull
    {
        return source
            .Skip(1)
            .Throttle(throttle ?? TimeSpan.FromSeconds(1), scheduler ?? Scheduler.Default)
            .DistinctUntilChanged()
            .WithLatestFrom(settingsStream, (data, settings) => (data, settings))
            .Where(x => x.settings is not null && x.data is not null)
            .Where(x => toggleSelector(x.settings))
            .Select(x =>
                Observable.FromAsync(() => saveAsync(x.data))
                    .Catch((Exception ex) =>
                    {
                        onError?.Invoke(ex);
                        return Observable.Empty<Unit>();
                    })
            )
            .Concat()
            .Subscribe();
    }
}