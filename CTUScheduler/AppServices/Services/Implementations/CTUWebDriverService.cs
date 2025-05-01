using CTUScheduler.AppServices.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTUScheduler.AppServices.Services.Implementations
{
    public class CTUWebDriverService: ICTUWebDriverService
    {
        private readonly IWebDriverService _webDriverService;

        public CTUWebDriverService(IWebDriverService webDriverService)
        {
            _webDriverService = webDriverService;
        }

        public async Task<bool> TrySignIn()
        {

            return false;
        }
    }
}
