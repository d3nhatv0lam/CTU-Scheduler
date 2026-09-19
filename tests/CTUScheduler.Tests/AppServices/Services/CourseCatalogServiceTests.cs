using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CTUScheduler.AppServices.Services.StudentAcademicService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Models.Shared.Results;
using CTUScheduler.Infrastructure.Sites.CTU.Abstractions;
using CTUScheduler.Infrastructure.Sites.CTU.Models.Curriculum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CTUScheduler.Tests.AppServices.Services;

public class CourseCatalogServiceTests
{
    private readonly ICourseCatalogClient _mockClient;
    private readonly Lazy<IUserSessionService> _mockUserSessionService;
    private readonly CourseCatalogService _service;

    public CourseCatalogServiceTests()
    {
        _mockClient = Substitute.For<ICourseCatalogClient>();
        var userSession = Substitute.For<IUserSessionService>();
        _mockUserSessionService = new Lazy<IUserSessionService>(() => userSession);
        _service = new CourseCatalogService(_mockClient, _mockUserSessionService, NullLogger<CourseCatalogService>.Instance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FetchSuggestionsAsync_WhenQueryEmpty_ShouldReturnEmptyList(string query)
    {
        var result = await _service.FetchSuggestionsAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().BeEmpty();
        await _mockClient.DidNotReceiveWithAnyArgs().GetAutoCompleteQueryAsync(default!);
    }

    [Fact]
    public async Task FetchSuggestionsAsync_WhenClientReturnsData_ShouldReturnSuccess()
    {
        var mockSuggestions = new List<QuickSelectDmhpCourse>
        {
            new("TH001", "Tin học đại cương")
        };
        _mockClient.GetAutoCompleteQueryAsync("TH001", Arg.Any<CancellationToken>())
                   .Returns(mockSuggestions);

        var result = await _service.FetchSuggestionsAsync("TH001");

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().HaveCount(1);
        result.Content![0].CourseCode.Should().Be("TH001");
    }

    [Fact]
    public async Task FetchSuggestionsAsync_WhenSessionExpired_ShouldReturnUnauthorizedFailure()
    {
        _mockClient.GetAutoCompleteQueryAsync("TH001", Arg.Any<CancellationToken>())
                   .Throws(new SessionExpiredException("Phiên đăng nhập hết hạn"));

        var result = await _service.FetchSuggestionsAsync("TH001");

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Unauthorized);
    }

    [Fact]
    public async Task FetchSuggestionsAsync_WhenNetworkFails_ShouldReturnNetworkFailure()
    {
        _mockClient.GetAutoCompleteQueryAsync("TH001", Arg.Any<CancellationToken>())
                   .Throws(new HttpRequestException("No connection"));

        var result = await _service.FetchSuggestionsAsync("TH001");

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Network);
    }
}
