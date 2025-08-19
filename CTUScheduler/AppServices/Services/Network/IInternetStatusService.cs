using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Network
{
    public interface IInternetStatusService
    {
        /// <summary>
        /// Kiểm tra trạng thái kết nối Internet
        /// </summary>
        /// <returns>Trả về true nếu có kết nối, false nếu không có kết nối.</returns>
        public Task<bool> GetInternetStatus();

        public event EventHandler<bool>? ConnectivityChanged;
        /// <summary>
        /// Sự kiện khi trạng thái kết nối: kết nối thành công
        /// </summary>
        public event EventHandler? InternetConnected;
        /// <summary>
        /// Sự kiện khi trạng thái kết nối: mất kết nối
        /// </summary>
        public event EventHandler? InternetDisconnected;
        /// <summary>
        /// Phát ra giá trị khả dụng của internet sau khoảng thời gian được định sẵn
        /// </summary>
        public Subject<bool> InternetStatusOnRefresh { get; }
    }
}
