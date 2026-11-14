using System;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using CTUScheduler.Presentation.Services.CrashReport;
using CTUScheduler.Presentation.Services.ViewContext.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Features.CrashReport;

public class CrashReportServiceTests
{
    private readonly CrashReportService _service = new(
        NullLogger<CrashReportService>.Instance,
        Substitute.For<IViewContextService>(),
        Substitute.For<IAppLifecycleService>());

    [Fact]
    public void IsMuted_ReturnsFalse_Initially()
    {
        var ex = new InvalidOperationException("Test error");
        _service.IsMuted(ex).Should().BeFalse();
    }

    [Fact]
    public void Mute_MakesIsMuted_ReturnTrue_ForSameError()
    {
        var ex = new InvalidOperationException("Test error");
        _service.Mute(ex);
        _service.IsMuted(ex).Should().BeTrue();
    }

    [Fact]
    public void Mute_DoesNotMute_DifferentExceptionType()
    {
        var ex1 = new InvalidOperationException("Test error");
        var ex2 = new ArgumentException("Test error");

        _service.Mute(ex1);

        _service.IsMuted(ex1).Should().BeTrue();
        _service.IsMuted(ex2).Should().BeFalse();
    }

    [Fact]
    public void CrashReporter_Show_Null_DoesNotThrow()
    {
        var act = () => CrashReporter.Show(null);
        act.Should().NotThrow();
    }
}
