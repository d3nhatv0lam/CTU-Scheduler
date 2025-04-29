using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IInternetStatusService
    {
        /// <summary>
        /// Kiểm tra trạng thái kết nối Internet
        /// </summary>
        /// <returns>Trả về true nếu có kết nối, false nếu không có kết nối.</returns>
        public Task<bool> CheckInternetStatus();

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
