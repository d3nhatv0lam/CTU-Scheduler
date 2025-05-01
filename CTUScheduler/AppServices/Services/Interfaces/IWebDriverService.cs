using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Interfaces
{
    public interface IWebDriverService
    {
        event EventHandler AlertBoxOpened;
        event EventHandler ConfirmBoxOpened;

        string GetPageUrl();
        Task GoToPage(string url);
        ILocator LocatorElement(string selector);
        Task FillElement(ILocator element, string strValue);
        Task FillElement(string selector, string strValue);
        Task ClickElement(ILocator element);
        Task ClickElement(string selector);
        Task ClickNavigateElement(ILocator element);
        Task ClickNavigateElement(string selector);
        Task<byte[]> GetImageToByteArray(ILocator element);
        Task<byte[]> GetImageToByteArray(string selector);
    }
}
