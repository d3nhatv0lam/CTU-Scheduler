using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Raw;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Registration;

public class RegistrationRulesService: IRegistrationRulesService
{
    private readonly ILogger<RegistrationRulesService> _logger;
    private readonly ICtuSitePageFactory _factory;
    private readonly IRegistrationRulesPage _rulesPage;
    
    public IObservable<RegistrationInformation> RegistrationInfoChanges { get; }
    public RegistrationRulesService(
        ICtuSitePageFactory factory,
        ILogger<RegistrationRulesService> logger)
    {
        _factory = factory;
        _logger = logger;
        _rulesPage = factory.GetPage<IRegistrationRulesPage>();
        
        RegistrationInfoChanges = _rulesPage.RawRegistrationInformationResponse
            .Where(x => x is {IsSuccess:true, Content: not null})
            .Select(x => x.Content!)
            .SelectMany(async (x, ct) => await ProcessRegistrationInfoAsync(x, ct))
            .Where(x => x is not null)
            .Select(x => x!)
            .Publish()
            .RefCount();
    }

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            if (await _rulesPage.IsActive.FirstAsync())
                return OperationResult.Success();
            
            await _rulesPage.NavigateToAsync(allowRedirection:false);
            
            var homePage = _factory.GetPage<IMainPage>();
            if (await homePage.TryWaitForActiveAsync( stabilityMs:1000,timeout:10000))
            {
                await homePage.NavigateToDkmhAsync();
                if (!await _rulesPage.TryWaitForActiveAsync()) 
                    throw new InvalidOperationException("Trang dkmh chưa được load xong");
            }
            return OperationResult.Success();
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult.Failed(ex.Message, OperationFailureReason.Network);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to navigate to registration rules page");
            return OperationResult.Failed("Hệ thống truy cập trang dkmh không thành công!", OperationFailureReason.System);
        }
    }
    
    private async Task<RegistrationInformation?> ProcessRegistrationInfoAsync(RawRegistrationInformation rawContent, CancellationToken token)
    {
        try 
        {
            var userInfo = await _rulesPage.TryGetUserKeyAndUnitAsync();
            return rawContent.ToRegistrationInformation(userInfo.userKey, userInfo.userUnit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse registration info or get user key.");
            return null; 
        }
    }
}