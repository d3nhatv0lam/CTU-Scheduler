using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions;

public static class RxExtensions
{
    /// <summary>
    /// AutoSave với Policy Selector chọn điều kiện bật/tắt từ một object Settings chung.
    /// </summary>
    public static IDisposable AutoSave<TData, TSettings>(
        this IObservable<TData> source,           
        IObservable<TSettings> settingsStream,    
        Func<TSettings, bool> toggleSelector,     
        Func<TSettings, TimeSpan> delaySelector,
        Func<TData, Task> saveAsync,              
        Action<Exception>? onError = null, 
        IScheduler? scheduler = null) 
        where TData : notnull
        where TSettings : notnull
    {
        return source
            .DistinctUntilChanged()
            .Skip(1)
            .WithLatestFrom(settingsStream, (data, settings) => (data, settings))
            .Where(x => toggleSelector(x.settings))
            .Throttle(x => Observable.Timer(delaySelector(x.settings), scheduler ?? Scheduler.Default))
            .WithLatestFrom(settingsStream, (prev, currentSettings) => (prev.data, currentSettings))
            .Where(x => toggleSelector(x.currentSettings))
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