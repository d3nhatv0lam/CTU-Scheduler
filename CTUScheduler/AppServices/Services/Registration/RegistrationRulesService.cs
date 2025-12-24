using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CTUScheduler.AppServices.State;
using CTUScheduler.Core.Extensions;
using CTUScheduler.Core.Interfaces.WebDriver.Sites.CTU;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration.Processed;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Infrastructure.Sites.CTU.Factory;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.AppServices.Services.Registration;

public class RegistrationRulesService: IRegistrationRulesService, IDisposable
{
    private readonly ILogger<RegistrationRulesService> _logger;
    private readonly ICtuSitePageFactory _factory;
    private readonly IRegistrationRulesPage _rulesPage;
    private readonly CompositeDisposable _disposables = new();
    
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
    
    
    public RegistrationRulesService(
        AppState appState,
        ICtuSitePageFactory factory,
        ILogger<RegistrationRulesService> logger)
    {
        _factory = factory;
        _logger = logger;

        _rulesPage = factory.GetPage<IRegistrationRulesPage>();
        RegistrationInfoChanges
            .Subscribe(appState.UpdateRegistrationInfo)
            .DisposeWith(_disposables);
    }

    public async Task<OperationResult> NavigateToAsync()
    {
        try
        {
            if (await _rulesPage.IsActive.FirstAsync())
                return OperationResult.Success();
            
            await _rulesPage.NavigateToAsync(allowRedirection:false);
            
            var homePage = _factory.GetPage<IMainPage>();
            if (await homePage.TryWaitForActiveAsync(1000,5000))
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

    public void Dispose()
    {
        _disposables.Dispose();
    }
}