using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CTUScheduler.Core.Algorithms;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.Core.Algorithms;

public class CombinatoricsTests
{
    [Fact]
    public void CartesianProduct_WhenSetsNull_ShouldThrowArgumentNullException()
    {
        Action act = () => Combinatorics.CartesianProduct<int>(null!).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CartesianProduct_WhenEmptySets_ShouldYieldNoElements()
    {
        var sets = Array.Empty<IReadOnlyList<int>>();

        var result = Combinatorics.CartesianProduct(sets).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void CartesianProduct_WhenAnyInnerSetIsEmpty_ShouldYieldNoElements()
    {
        var sets = new List<IReadOnlyList<int>>
        {
            new List<int> { 1, 2 },
            new List<int>(), // empty
            new List<int> { 3, 4 }
        };

        var result = Combinatorics.CartesianProduct(sets).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void CartesianProduct_BasicCartesian_ShouldProduceAllCombinations()
    {
        // 2 x 2 x 2 = 8 combinations
        var sets = new List<IReadOnlyList<string>>
        {
            new List<string> { "A1", "A2" },
            new List<string> { "B1", "B2" },
            new List<string> { "C1", "C2" }
        };

        var results = Combinatorics.CartesianProduct(sets).ToList();

        results.Should().HaveCount(8);
        results.Select(r => string.Join("-", r)).Should().BeEquivalentTo(new[]
        {
            "A1-B1-C1", "A1-B1-C2", "A1-B2-C1", "A1-B2-C2",
            "A2-B1-C1", "A2-B1-C2", "A2-B2-C1", "A2-B2-C2"
        });
    }

    [Fact]
    public void CartesianProduct_WithCandidatePruning_ShouldPruneBranchesEarly()
    {
        // Combinations of numbers where no two numbers sum to 10
        var sets = new List<IReadOnlyList<int>>
        {
            new List<int> { 1, 5 },
            new List<int> { 2, 5 },
            new List<int> { 9, 3 }
        };

        int validationCallCount = 0;

        var results = Combinatorics.CartesianProduct(
            sets,
            isValidCandidate: (currentPath, candidate) =>
            {
                validationCallCount++;
                foreach (var item in currentPath)
                {
                    if (item + candidate == 10) return false;
                }
                return true;
            }
        ).ToList();

        results.Should().NotBeEmpty();
        foreach (var combo in results)
        {
            (combo[0] + combo[1]).Should().NotBe(10);
            (combo[0] + combo[2]).Should().NotBe(10);
            (combo[1] + combo[2]).Should().NotBe(10);
        }
        validationCallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CartesianProduct_WithFullValidator_ShouldFilterFinalResults()
    {
        var sets = new List<IReadOnlyList<int>>
        {
            new List<int> { 1, 2 },
            new List<int> { 3, 4 }
        };

        // Only accept combinations whose sum is even
        var results = Combinatorics.CartesianProduct(
            sets,
            isValidFull: fullPath => (fullPath[0] + fullPath[1]) % 2 == 0
        ).ToList();

        // (1+3=4 - even), (2+4=6 - even). (1+4=5, 2+3=5 - odd rejected)
        results.Should().HaveCount(2);
        results.Should().BeEquivalentTo(new[]
        {
            new[] { 1, 3 },
            new[] { 2, 4 }
        });
    }

    [Fact]
    public void CartesianProduct_WhenCancelled_ShouldStopImmediately()
    {
        using var cts = new CancellationTokenSource();
        var largeSet = Enumerable.Range(0, 10).ToList();
        var sets = Enumerable.Repeat(largeSet, 10).Cast<IReadOnlyList<int>>().ToList();

        var yielded = 0;
        foreach (var _ in Combinatorics.CartesianProduct(sets, token: cts.Token))
        {
            yielded++;
            if (yielded == 5)
            {
                cts.Cancel();
            }
        }

        yielded.Should().BeLessThan(100);
    }

    [Fact]
    public void CartesianProduct_SingleSet_ShouldYieldAllElementsAsSingleItemArrays()
    {
        var sets = new List<IReadOnlyList<string>>
        {
            new List<string> { "X1", "X2", "X3" }
        };

        var results = Combinatorics.CartesianProduct(sets).ToList();

        results.Should().HaveCount(3);
        results[0].Should().Equal("X1");
        results[1].Should().Equal("X2");
        results[2].Should().Equal("X3");
    }

    [Fact]
    public void CartesianProduct_WhenAllCandidatesPruned_ShouldYieldEmpty()
    {
        var sets = new List<IReadOnlyList<int>>
        {
            new List<int> { 1, 2 },
            new List<int> { 3, 4 }
        };

        // Reject everything
        var results = Combinatorics.CartesianProduct(
            sets,
            isValidCandidate: (_, _) => false
        ).ToList();

        results.Should().BeEmpty();
    }
}
