using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Registration;

public class RegistrationRulesService: IRegistrationRulesService
{
    private readonly ILogger<RegistrationRulesService> _logger;
    private readonly ICtuSitePageFactory _factory;
    private readonly IRegistrationRulesPage _rulesPage;
    

    public IObservable<RegistrationInformation> RegistrationInfoChanges => 
        _rulesPage.RawRegistrationInformationResponse
        .Where(x => x.IsSuccess)
        .Select(x => x.Content)
        .SelectMany(async raw =>
        {
            try
            {
                var userInfo = await _rulesPage.TryGetUserKeyAndUnitAsync();
                return raw?.ToRegistrationInformation(userInfo.userKey, userInfo.userUnit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to get user key and unit");
                return null;
            }
        })
        .Where(reg => reg is not null)!;
    
    
    public RegistrationRulesService(ICtuSitePageFactory factory, ILogger<RegistrationRulesService> logger)
    {
        _factory = factory;
        _logger = logger;

        _rulesPage = factory.GetPage<IRegistrationRulesPage>();
    }

    public async Task<OperationResult> NavigateToAsync()
    {
        try
        {
            var homePage = _factory.GetPage<IMainPage>();
            // chờ trang home xuất hiện
            throw new NotImplementedException();
            if (await homePage.IsActive.FirstAsync())
            {
                await homePage.NavigateToDkmhAsync();
            }
            await _rulesPage.NavigateToAsync();
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to navigate to registration rules page");
            return OperationResult.Failed("Hệ thống truy cập trang dkmh không thành công!", OperationFailureReason.System);
        }
    }
}