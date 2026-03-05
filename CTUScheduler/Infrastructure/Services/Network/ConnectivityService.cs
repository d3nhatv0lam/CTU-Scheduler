using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Network;

public class ConnectivityService: IConnectivityService, IDisposable
{
    private const string PRIMARY_URI = "http://connectivitycheck.gstatic.com/generate_204";
    private const string BACKUP_URI = "http://www.msftconnecttest.com/connecttest.txt";
    
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<ConnectivityService> _logger;
    private readonly BehaviorSubject<bool> _internetSubject;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);
    public IObservable<bool> IsInternetAvailable { get; }

    public ConnectivityService(ILogger<ConnectivityService> logger, IScheduler? scheduler = null)
    {
        _logger = logger;
        var schedulerUsed = scheduler ?? Scheduler.Default;
        
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        
        // trạng thái đầu tiên là hỏi hệ điều hành
        _internetSubject = new BehaviorSubject<bool>(NetworkInterface.GetIsNetworkAvailable());
        IsInternetAvailable = _internetSubject.AsObservable();
        
        var networkChangeStream = Observable.FromEventPattern<NetworkAvailabilityChangedEventHandler, NetworkAvailabilityEventArgs>(
                h => NetworkChange.NetworkAvailabilityChanged += h,
                h => NetworkChange.NetworkAvailabilityChanged -= h
            )
            .Throttle(TimeSpan.FromMilliseconds(500), schedulerUsed)
            .Select(_ => Unit.Default);
        
        var timerStream = Observable.Interval(_checkInterval, schedulerUsed)
            .Select(_ => Unit.Default);

        networkChangeStream.Merge(timerStream)
            .StartWith(Unit.Default)
            .ObserveOn(schedulerUsed)
            .Select(_ => Observable.FromAsync(ct => CheckInternetAccessAsync(ct))
                .Catch((Exception ex) => 
                {
                    _logger.LogWarning(ex, "Failed to check internet connectivity");                    
                    return Observable.Return(false); 
                }))
            .Switch()
            .DistinctUntilChanged()
            .Subscribe(isConnected =>
            {
                _internetSubject.OnNext(isConnected);
                _logger.LogInformation($"Internet status changed to {isConnected}");
            }, ex =>
            {
                _logger.LogCritical(ex, "ConnectivityService CRASHED - Timer stopped!");
                // nếu lỗi thì coi như mất mạng
                _internetSubject.OnNext(false);
            })
            .DisposeWith(_disposables);
        
        _internetSubject.DisposeWith(_disposables);
        _httpClient.DisposeWith(_disposables);
    }

    public Task<bool> CheckInternetAccessAsync()
    {
        return CheckInternetAccessAsync(CancellationToken.None);
    }
    private async Task<bool> CheckInternetAccessAsync(CancellationToken token)
    {
        // hỏi hệ điều hành trước
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;
        
        // có thì ping thử xem có mạng thật không?
        if (await ProbeUrl(PRIMARY_URI, HttpStatusCode.NoContent, token))
        {
            return true;
        }

        // BACKUP_URI này trả về 200 OK
        return await ProbeUrl(BACKUP_URI, HttpStatusCode.OK, token);
    }
    
    private async Task<bool> ProbeUrl(string url, HttpStatusCode expectedCode, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            
            using var timeoutCts = new CancellationTokenSource(2000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
            
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            return response.StatusCode == expectedCode;
        }
        catch
        {
            return false;
        }
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogInformation("Connectivity service disposed!");
    }
}