using System;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Network;
public class InternetStatusService : IInternetStatusService, IDisposable
{
    protected const string ENDPOINT_INTERENET_CHECK = "https://www.google.com/generate_204";
    private readonly CompositeDisposable _disposable = new ();
    private readonly Subject<bool> _internetStatusOnRefresh = new();
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Phát ra giá trị trạng thái Internet được làm mới
    /// </summary>
    public IObservable<bool> InternetStatusOnRefresh => _internetStatusOnRefresh.AsObservable();

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
    public InternetStatusService(TimeSpan interval)
    {
        _httpClient = new HttpClient()
        {
            Timeout = interval
        };

        // Tạo một observable để gửi kiểm tra định kỳ
         Observable.Interval(interval)
                // Mỗi tick thì chuyển đổi sang Task kiểm tra kết nối rồi chuyển kết quả về là Observable<bool>
                .SelectMany(async _ => await CheckInternetAsync().ToObservable())
                // Update InternetStatusOnRefresh
                .Do(status => _internetStatusOnRefresh.OnNext(status))
                // Chỉ thông báo khi trạng thái thay đổi (không thông báo liên tục cùng trạng thái)
                .DistinctUntilChanged()
                .Subscribe(status => OnConnectivityChanged(status)
                ).DisposeWith(_disposable);

        _disposable.Add(_internetStatusOnRefresh);
        _disposable.Add(_httpClient);
    }


    public Task<bool> GetInternetStatus()
    {
        return CheckInternetAsync();
    }

    /// <summary>
    /// Phương thức kiểm tra kết nối bằng cách gửi yêu cầu HTTP đến một endpoint đáng tin cậy.
    /// </summary>
    private async Task<bool> CheckInternetAsync()
    {
        try
        {
            // Endpoint có thể thay đổi tuỳ ứng dụng, ở đây dùng google.com làm ví dụ.
            using var requestMessage = new HttpRequestMessage(HttpMethod.Head, ENDPOINT_INTERENET_CHECK);
            var response = await _httpClient.SendAsync(requestMessage);
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
        InternetConnected?.Invoke(this,EventArgs.Empty);
    }

    protected virtual void OnInternetDisconnected() 
    {
        InternetDisconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
