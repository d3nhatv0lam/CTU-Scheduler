using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTUScheduler.Core.Exceptions;
using System.Reactive.Subjects;
using System.Text.Json;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IWebDriverService
    {

        Subject<JsonElement?> JsonResponse { get; }

        event EventHandler AlertBoxOpened;
        event EventHandler ConfirmBoxOpened;
        ///<summary>
        /// Throw NoInternetException Exeption when no Internet 
        /// </summary>
        /// <exception cref="NoInternetException">
        /// </exception>
        void EnsureInternetConnection();
        string GetPageUrl();
        Task GoToPageAsync(string url);
        Task RefreshPageAsync();
        ILocator LocatorElement(string selector);
        Task FillElementAsync(ILocator element, string strValue);
        Task FillElementAsync(string selector, string strValue);
        Task ClickElementAsync(ILocator element);
        Task ClickElementAsync(string selector);
        Task ClickNavigateElementAsync(ILocator element, LoadState loadState = LoadState.Load);
        Task ClickNavigateElementAsync(string selector, LoadState loadState = LoadState.Load);
        Task<byte[]> GetImageToByteArrayAsync(ILocator element);
        Task<byte[]> GetImageToByteArrayAsync(string selector);
    }
}
