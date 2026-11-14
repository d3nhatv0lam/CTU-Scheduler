using System;
using System.Reactive.Linq;
using CTUScheduler.Presentation.Features.CrashReport.ViewModels;
using CTUScheduler.Presentation.Services.ApplicationLifetime;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CTUScheduler.Tests.Presentation.Features.CrashReport;

public class CrashReportViewModelTests
{
    [Fact]
    public void WhenIsFatalIsTrue_PropertiesAreConfiguredForFatalCrash()
    {
        // Arrange
        var ex = new InvalidOperationException("Fatal database error");

        // Act
        var vm = new CrashReportViewModel(ex, isFatal: true);

        // Assert
        vm.IsFatal.Should().BeTrue();
        vm.WindowTitle.Should().Contain("nghiêm trọng");
        vm.HeaderTitle.Should().Contain("NGHIÊM TRỌNG");
        vm.HeaderSubtitle.Should().Contain("không thể tiếp tục hoạt động");
        vm.ErrorDetailsText.Should().Contain("InvalidOperationException");
        vm.ErrorDetailsText.Should().Contain("Fatal database error");
        vm.ErrorDetailsText.Should().Contain("Hệ điều hành:");
        vm.ErrorDetailsText.Should().Contain("Phiên bản:");
    }

    [Fact]
    public void WhenIsFatalIsFalse_PropertiesAreConfiguredForNonFatalError()
    {
        // Arrange
        var ex = new ArgumentException("Invalid course selection");

        // Act
        var vm = new CrashReportViewModel(ex, isFatal: false);

        // Assert
        vm.IsFatal.Should().BeFalse();
        vm.WindowTitle.Should().Contain("lỗi tác vụ");
        vm.HeaderTitle.Should().Contain("LỖI TÁC VỤ");
        vm.HeaderSubtitle.Should().Contain("tiếp tục");
        vm.ErrorDetailsText.Should().Contain("ArgumentException");
        vm.ErrorDetailsText.Should().Contain("Invalid course selection");
    }

    [Fact]
    public void WhenExceptionIsNull_HandlesGracefullyWithoutThrowing()
    {
        // Act
        var vm = new CrashReportViewModel(null, isFatal: false);

        // Assert
        vm.ErrorDetailsText.Should().Contain("Không có thông tin ngoại lệ");
        vm.ErrorDetailsText.Should().Contain("Thời gian:");
    }

    [Fact]
    public void MuteThisError_DefaultsToFalse_AndCanBeToggled()
    {
        // Act
        var vm = new CrashReportViewModel(new Exception("test"), isFatal: false);

        // Assert
        vm.MuteThisError.Should().BeFalse();

        // Act
        vm.MuteThisError = true;

        // Assert
        vm.MuteThisError.Should().BeTrue();
    }

    [Fact]
    public void RestartAppCommand_CallsAppLifecycleServiceRestart()
    {
        // Arrange
        var appLifecycle = Substitute.For<IAppLifecycleService>();
        var vm = new CrashReportViewModel(new Exception("test"), isFatal: true, appLifecycle);

        // Act
        vm.RestartAppCommand.Execute().Subscribe();

        // Assert
        appLifecycle.Received(1).Restart();
    }

    [Fact]
    public void CloseCommand_WhenFatal_CallsAppLifecycleServiceShutdown()
    {
        // Arrange
        var appLifecycle = Substitute.For<IAppLifecycleService>();
        var vm = new CrashReportViewModel(new Exception("test"), isFatal: true, appLifecycle);

        // Act
        vm.CloseCommand.Execute().Subscribe();

        // Assert
        appLifecycle.Received(1).Shutdown();
    }
}
