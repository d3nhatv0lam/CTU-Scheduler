using Avalonia.Data.Converters;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations;
public class InternetStatusService : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly HttpClient _httpClient;
    private bool _lastStatus;
    /// <summary>
    /// Phát ra giá trị khi Internet có thay đổi
    /// </summary>
    public readonly Subject<bool> IsInternetAvaiable = new Subject<bool>();
    /// <summary>
    /// Phát ra giá trị trạng thái Internet được làm mới
    /// </summary>
    public readonly Subject<bool> InternetStatusOnRefesh = new Subject<bool>();

    /// <summary>
    /// Sự kiện thay đổi trạng thái kết nối, truyền vào giá trị boolean (true: có kết nối, false: mất kết nối).
    /// </summary>
    public event EventHandler<bool>? ConnectivityChanged;
    /// <summary>
    /// sự kiện khi trạng thái kết nối: kết nối thành công
    /// </summary>
    public event EventHandler? InternetConnected;
    /// <summary>
    /// sự kiện khi trạng thái kết nối: mất kết nối
    /// </summary>
    public event EventHandler? InternetDisconnected;

    /// <summary>
    /// Khởi tạo monitor với khoảng thời gian kiểm tra nhất định.
    /// </summary>
    /// <param name="interval">Khoảng thời gian giữa các lần kiểm tra.</param>
    private InternetStatusService(TimeSpan interval)
    {
        _httpClient = new HttpClient()
        {
            Timeout = interval
        };

        // Tạo một observable để gửi kiểm tra định kỳ
        _subscription = Observable.Interval(interval)
            // Mỗi tick thì chuyển đổi sang Task kiểm tra kết nối rồi chuyển kết quả về là Observable<bool>
            .SelectMany(async _ => await CheckInternetAsync().ToObservable())
            // Update InternetStatusOnRefesh
            .Do(status => InternetStatusOnRefesh.OnNext(status))
            // Chỉ thông báo khi trạng thái thay đổi (không thông báo liên tục cùng trạng thái)
            .DistinctUntilChanged()
            .Subscribe(status =>
            {
                _lastStatus = status;
                IsInternetAvaiable.OnNext(status);
                OnConnectivityChanged(status);
            });
    }

    public static InternetStatusService CreateInstance(TimeSpan interval)
    {
        return new InternetStatusService(interval);
    }

    public async Task<bool> CheckInternetStatus()
    {
        return await CheckInternetAsync();
    }

    /// <summary>
    /// Phương thức kiểm tra kết nối bằng cách gửi yêu cầu HTTP đến một endpoint đáng tin cậy.
    /// </summary>
    private async Task<bool> CheckInternetAsync()
    {
        try
        {
            // Endpoint có thể thay đổi tuỳ ứng dụng, ở đây dùng google.com làm ví dụ.
            var response = await _httpClient.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    protected virtual void OnConnectivityChanged(bool status)
    {
        ConnectivityChanged?.Invoke(this, status);
        // run Event
        if (status)
            OnInternetConnected();
        else 
            OnInternetDisconnected();
    }

    protected virtual void OnInternetConnected() 
    {
        InternetConnected?.Invoke(this,new EventArgs());
    }

    protected virtual void OnInternetDisconnected() 
    {
        InternetDisconnected?.Invoke(this,new EventArgs());
    }

    public void Dispose()
    {
        IsInternetAvaiable.Dispose();
        _subscription.Dispose();
        _httpClient.Dispose();
    }
}
