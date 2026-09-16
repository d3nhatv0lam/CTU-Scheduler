using System;
using System.Linq;
using CTUScheduler.Core.Models.Timetable;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Models;

public class TimetableMaskTests
{
    [Fact]
    public void Create_SingleSlot_ShouldSetCorrectOccupancy()
    {
        var mask = TimetableMask.Create(day: 2, period: 1);

        mask.IsOccupied(day: 2, period: 1).Should().BeTrue();
        mask.IsOccupied(day: 2, period: 2).Should().BeFalse();
        mask.IsOccupied(day: 3, period: 1).Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 1)]   // Day < 2
    [InlineData(9, 1)]   // Day > 8
    [InlineData(2, 0)]   // Period < 1
    [InlineData(2, 14)]  // Period > 13
    public void Create_InvalidSlot_ShouldThrowArgumentOutOfRangeException(int day, int period)
    {
        Action act = () => TimetableMask.Create(day, period);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Overlaps_WhenSlotsCollide_ShouldReturnTrue()
    {
        var mask1 = TimetableMask.Create([(2, 1), (2, 2), (3, 5)]);
        var mask2 = TimetableMask.Create([(2, 2), (4, 1)]);

        var overlaps = mask1.Overlaps(mask2);

        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WhenSlotsDoNotCollide_ShouldReturnFalse()
    {
        var mask1 = TimetableMask.Create([(2, 1), (2, 2)]);
        var mask2 = TimetableMask.Create([(2, 3), (3, 1)]);

        var overlaps = mask1.Overlaps(mask2);

        overlaps.Should().BeFalse();
    }

    [Fact]
    public void BitwiseOr_ShouldCombineSlots()
    {
        var mask1 = TimetableMask.Create(2, 1);
        var mask2 = TimetableMask.Create(3, 4);

        var combined = mask1 | mask2;

        combined.IsOccupied(2, 1).Should().BeTrue();
        combined.IsOccupied(3, 4).Should().BeTrue();
        combined.IsOccupied(2, 2).Should().BeFalse();
    }

    [Fact]
    public void BitwiseAnd_ShouldKeepOnlyIntersection()
    {
        var mask1 = TimetableMask.Create([(2, 1), (2, 2), (3, 3)]);
        var mask2 = TimetableMask.Create([(2, 2), (3, 3), (4, 4)]);

        var intersection = mask1 & mask2;

        intersection.IsOccupied(2, 2).Should().BeTrue();
        intersection.IsOccupied(3, 3).Should().BeTrue();
        intersection.IsOccupied(2, 1).Should().BeFalse();
        intersection.IsOccupied(4, 4).Should().BeFalse();
    }

    [Fact]
    public void GetOccupiedSlots_ShouldEnumerateExactSlots()
    {
        var expectedSlots = new (int Day, int Period)[]
        {
            (2, 1),
            (2, 5),
            (5, 10),
            (8, 13)
        };
        var mask = TimetableMask.Create(expectedSlots);

        var actualSlots = mask.GetOccupiedSlots().ToList();

        actualSlots.Should().BeEquivalentTo(expectedSlots);
    }

    [Fact]
    public void Combine_MultipleMasks_ShouldContainAllSlots()
    {
        var mask1 = TimetableMask.Create(2, 1);
        var mask2 = TimetableMask.Create(3, 2);
        var mask3 = TimetableMask.Create(4, 3);

        var combined = TimetableMask.Combine(mask1, mask2, mask3);

        combined.IsOccupied(2, 1).Should().BeTrue();
        combined.IsOccupied(3, 2).Should().BeTrue();
        combined.IsOccupied(4, 3).Should().BeTrue();
        combined.GetOccupiedSlots().Should().HaveCount(3);
    }

    [Fact]
    public void Boundaries_MinAndMaxValidSlots_ShouldSucceed()
    {
        // Min boundary: Monday period 1
        var minMask = TimetableMask.Create(2, 1);
        minMask.IsOccupied(2, 1).Should().BeTrue();

        // Max boundary: Sunday period 13
        var maxMask = TimetableMask.Create(8, 13);
        maxMask.IsOccupied(8, 13).Should().BeTrue();
    }

    [Fact]
    public void CountOccupied_ShouldMatchNumberOfSlots()
    {
        var mask = TimetableMask.Create([(2, 1), (2, 2), (5, 5)]);

        mask.CountOccupied().Should().Be(3);
    }

    [Fact]
    public void AnyOverlap_WhenMultipleMasksHaveOverlap_ShouldReturnTrue()
    {
        var masks = new[]
        {
            TimetableMask.Create(2, 1),
            TimetableMask.Create(3, 1),
            TimetableMask.Create([(2, 1), (4, 1)]) // Overlaps first mask!
        };

        masks.AnyOverlap().Should().BeTrue();
    }

    [Fact]
    public void AnyOverlap_WhenNoMasksOverlap_ShouldReturnFalse()
    {
        var masks = new[]
        {
            TimetableMask.Create(2, 1),
            TimetableMask.Create(3, 1),
            TimetableMask.Create(4, 1)
        };

        masks.AnyOverlap().Should().BeFalse();
    }

    [Fact]
    public void ToDebugGrid_ShouldContainAsciiRepresentation()
    {
        var mask = TimetableMask.Create(2, 1);

        var grid = mask.ToDebugGrid();

        grid.Should().NotBeNullOrWhiteSpace();
        grid.Should().Contain("T2 | █");
    }
}
