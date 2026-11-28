using CTUScheduler.AppServices.Services.StudentAcademicService;
using CTUScheduler.AppServices.Services.UserSessionService;
using CTUScheduler.Core.Exceptions;
using CTUScheduler.Core.Interfaces;
using CTUScheduler.Core.Models.Academic.Grades;
using CTUScheduler.Core.Models.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CTUScheduler.Tests.AppServices.Services;

public class GradeServiceTests
{
    private readonly IAcademicGradeClient _mockClient;
    private readonly GradeStore _gradeStore;
    private readonly GradeService _service;

    public GradeServiceTests()
    {
        _mockClient = Substitute.For<IAcademicGradeClient>();
        _gradeStore = new GradeStore(NullLogger<GradeStore>.Instance);
        _service = new GradeService(_mockClient, _gradeStore, NullLogger<GradeService>.Instance);
    }

    private static GradeTranscript CreateDummyTranscript()
    {
        var courses = new List<CourseGrade>
        {
            new(1, "CT100", "Kỹ năng học đại học", false, "D01", 2, "A", GradeLetter.A, 9.4, null, true)
        };
        var summary = new SemesterGradeSummary(2, 2, 4.0, 4.0, 83);
        var semesters = new List<SemesterGrade>
        {
            new(2023, 1, courses, summary)
        };
        return new GradeTranscript(semesters, new DateTimeOffset(2026, 11, 28, 14, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenClientSucceeds_UpdatesStoreAndReturnsSuccess()
    {
        var transcript = CreateDummyTranscript();
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Returns(transcript);

        var result = await _service.RefreshGradesAsync();

        result.IsSuccess.Should().BeTrue();
        _gradeStore.Current.Should().Be(transcript);
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenSessionExpired_ReturnsUnauthorizedAndPreservesExistingStore()
    {
        var existing = CreateDummyTranscript();
        _gradeStore.Update(existing);
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new SessionExpiredException("Expired"));

        var result = await _service.RefreshGradesAsync();

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Unauthorized);
        _gradeStore.Current.Should().Be(existing);
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenCaptchaBlocked_ReturnsNetworkFailureWithCaptchaCode()
    {
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new CaptchaBlockedException("Captcha"));

        var result = await _service.RefreshGradesAsync();

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Network);
        result.Errors.Should().ContainSingle(e => e.Code == "Grades.CaptchaBlocked");
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenHttpRequestFails_ReturnsNetworkFailure()
    {
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Offline"));

        var result = await _service.RefreshGradesAsync();

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Network);
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenCanceledByCaller_ReturnsUserActionFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException(cts.Token));

        var result = await _service.RefreshGradesAsync(cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.UserAction);
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenTimeoutOccurs_ReturnsNetworkFailure()
    {
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        var result = await _service.RefreshGradesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.Network);
        result.Errors.Should().ContainSingle(e => e.Code == "Grades.Timeout");
    }

    [Fact]
    public async Task RefreshGradesAsync_WhenInvalidData_ReturnsSystemFailure()
    {
        _mockClient.FetchTranscriptAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidDataException("Empty HTML"));

        var result = await _service.RefreshGradesAsync();

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(OperationFailureReason.System);
        result.Errors.Should().ContainSingle(e => e.Code == "Grades.InvalidData");
    }

    [Fact]
    public void GradeStore_Cleanup_ClearsCurrentTranscript()
    {
        _gradeStore.Update(CreateDummyTranscript());
        _gradeStore.Current.Should().NotBeNull();

        _gradeStore.Cleanup();

        _gradeStore.Current.Should().BeNull();
    }
}
