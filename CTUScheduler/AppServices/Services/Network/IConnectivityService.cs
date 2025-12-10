using System;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Network
{
    public interface IConnectivityService
    {
        /// <summary>
        /// Kiểm tra trạng thái kết nối Internet
        /// </summary>
        /// <returns>Trả về true nếu có kết nối, false nếu không có kết nối.</returns>
        public Task<bool> CheckInternetAccessAsync();
        /// <summary>
        /// Phát ra giá trị khả dụng của internet sau khoảng thời gian được định sẵn
        /// </summary>
        public IObservable<bool> IsInternetAvailable { get; }
    }
}
