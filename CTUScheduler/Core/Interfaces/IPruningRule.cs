using System.Collections.Generic;
using CTUScheduler.Core.Models.Shared;

namespace CTUScheduler.Core.Interfaces;

public interface IPruningRule
{
    /// <summary>
    /// Determines whether the addition of the next candidate section choice can proceed
    /// given the current path of section choices.
    /// </summary>
    /// <param name="currentPath">The current list of section choices representing the path being constructed.</param>
    /// <param name="nextCandidate">The next candidate section choice to evaluate for inclusion in the path.</param>
    /// <returns>
    /// A boolean value indicating whether the addition of the next candidate is valid
    /// based on specific pruning rules. Returns true if the addition can proceed, otherwise false.
    /// </returns>
    bool CanContinue(IReadOnlyList<SectionChoice> currentPath, SectionChoice nextCandidate);
}