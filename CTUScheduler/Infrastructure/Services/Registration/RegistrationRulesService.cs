using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Abstractions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Academic.Curriculum.Registration;
using CTUScheduler.Core.Models.Shared;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.DriverCore.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Extensions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using Microsoft.Extensions.Logging;

namespace CTUScheduler.Infrastructure.Services.Registration;

public class RegistrationRulesService : IRegistrationRulesService
{
    private static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(10);
    private readonly ILogger<RegistrationRulesService> _logger;
    private readonly ICtuPageFactory _factory;
    private readonly IWebDriverService _webDriverService;
    private readonly IRegistrationRulesPage _rulesPage;

    public RegistrationRulesService(
        ICtuPageFactory factory,
        IWebDriverService webDriverService,
        ILogger<RegistrationRulesService> logger)
    {
        _factory = factory;
        _logger = logger;
        _webDriverService = webDriverService;
        _rulesPage = _factory.GetPage<IRegistrationRulesPage>(webDriverService.MainTab);

        RegistrationInfoChanged = _rulesPage.RawRegistrationInformationResponse
                .SelectMany(async (x, ct) => await ProcessRegistrationInfoAsync(x, ct))
                .Where(x => x is not null)
                .Select(x => x!)
                .Replay(1)
                .RefCount();
    }

    public IObservable<RegistrationInformation> RegistrationInfoChanged { get; }
    
    // public async Task<OperationResult> EnsureReadyAsync() => OperationResult.Success();

    public async Task<OperationResult> EnsureReadyAsync()
    {
        try
        {
            var mainPage = _factory.GetPage<IMainPage>(_webDriverService.MainTab);
            if (await mainPage.IsActiveAsync())
            {
                await mainPage.WaitForReadyAsync();
                await mainPage.NavigateToDkmhAsync();
                await _rulesPage.WaitForReadyAsync();
                await _rulesPage.CheckSessionAndThrowAsync();
                return OperationResult.Success();
            }
    
            await _rulesPage.NavigateToAsync();
            await _rulesPage.WaitForReadyAsync();
            await _rulesPage.CheckSessionAndThrowAsync();
            
            return OperationResult.Success();
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Network);
        }
        catch (SessionExpiredException ex)
        {
            return OperationResult.Failed(ex.Message, kind: OperationFailureReason.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to navigate to registration rules page");
            return OperationResult.Failed("Hệ thống truy cập trang dkmh không thành công!",
                kind: OperationFailureReason.Unauthorized);
        }
    }

    public async Task<RegistrationInformation> FetchRegistrationInfoAsync(CancellationToken cancellationToken = default
        , TimeSpan? timeout = null)
    {
        var finalTimeout = timeout ?? DefaultFetchTimeout;

        return await RegistrationInfoChanged
            .Timeout(finalTimeout)
            .FirstAsync()
            .Timeout(finalTimeout)
            .ToTask(cancellationToken);
    }

    private async Task<RegistrationInformation?> ProcessRegistrationInfoAsync(DkmhQddkCrawlerPayload rawContent,
        CancellationToken token)
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