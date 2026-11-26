using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.CtuSessions;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.AppServices.Services;

public class SessionCoordinatorTests
{
    [Fact]
    public async Task StartSessionAsync_WhenAuthenticateThrowsCaptchaBlocked_ReturnsCaptchaRequiredFailure()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient.PingAuthServerAsync(Arg.Any<CancellationToken>())
            .Returns(AuthServerStatus.Healthy);
        authClient.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<CTUScheduler.Core.Networking.CtuSession>>(_ =>
                throw new CaptchaBlockedException("Kẹt Captcha, hãy bật VPN hoặc đổi mạng"));

        var sessionStore = Substitute.For<ICtuSessionStore>();
        var heartbeatService = Substitute.For<ISessionHeartbeatService>();
        heartbeatService.SessionExpired.Returns(Observable.Never<Unit>());

        var coordinator = new SessionCoordinator(
            authClient,
            sessionStore,
            heartbeatService,
            new Lazy<IEnumerable<ICleanup>>(() => []),
            new Lazy<IEnumerable<ICleanupAsync>>(() => []),
            Substitute.For<ILogger<SessionCoordinator>>());

        var result = await coordinator.StartSessionAsync("B2000001", "secret");

        result.IsFailed.Should().BeTrue();
        result.Kind.Should().Be(OperationFailureReason.Forbidden);
        result.Errors.Should().Contain(e => e.Code == "Auth.CaptchaRequired");
    }
}
