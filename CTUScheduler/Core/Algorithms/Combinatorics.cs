using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CTUScheduler.Core.Algorithms;

public static class Combinatorics
{
    // O(∏(sets[i].size)) 
    public static IEnumerable<List<T>> CartesianProduct<T>(
        IEnumerable<IReadOnlyList<T>> sets,
        Func<IReadOnlyList<T>, T, bool>? isValidCandidate = null,
        Func<IReadOnlyList<T>, bool>? isValidFull = null,
        CancellationToken? token = null)
    {
        var setList = sets as IReadOnlyList<IReadOnlyList<T>> ?? sets.ToList();
        if (setList.Count == 0 || setList.Any(s => s?.Count == 0))
            yield break;

        var indices = new int[setList.Count];
        var current = new List<T>(setList.Count);

        int depth = 0;
        Array.Fill(indices, -1);

        while (depth >= 0)
        {
            if (token?.IsCancellationRequested == true)
                yield break;

            indices[depth]++;

            if (indices[depth] >= setList[depth].Count)
            {
                indices[depth] = -1;
                depth--;
                if (depth >= 0 && current.Count > depth)
                    current.RemoveAt(current.Count - 1);
                continue;
            }

            T candidate = setList[depth][indices[depth]];

            if (isValidCandidate is not null && !isValidCandidate(current, candidate))
            {
                continue;
            }

            if (current.Count > depth)
                current[depth] = candidate;
            else
                current.Add(candidate);

            if (depth == setList.Count - 1)
            {
                if (isValidFull is null || isValidFull(current))
                    yield return new List<T>(current);
            }
            else
            {
                depth++;
            }
        }
    }

    public static IEnumerable<T[]> CartesianProductArray<T>(
        T[][] sets,
        Func<T[], int, T, bool>? isValidCandidate = null,
        Func<T[], bool>? isValidFull = null,
        CancellationToken token = default)
    {
        if (sets is null || sets.Length == 0) yield break;
        int count = sets.Length;
        for (int i = 0; i < count; i++)
        {
            if (sets[i] is null || sets[i].Length == 0) yield break;
        }
        
        var indices = new int[count];
        Array.Fill(indices, -1);

        var currentBuffer = new T[count]; 
        int depth = 0;

     
        while (depth >= 0)
        {
            if (token.IsCancellationRequested) yield break;

            T[] currentSet = sets[depth];
            indices[depth]++;
            
            if (indices[depth] >= currentSet.Length)
            {
                indices[depth] = -1;
                depth--;
                continue;
            }
            
            T candidate = currentSet[indices[depth]];

        
            if (isValidCandidate is not null)
            {
                if (!isValidCandidate(currentBuffer, depth, candidate))
                {
                    continue;
                }
            }
            
            currentBuffer[depth] = candidate;
            
            if (depth == count - 1)
            {
                if (isValidFull is null || isValidFull(currentBuffer))
                {
                    var result = new T[count];
                    Array.Copy(currentBuffer, result, count);
                    yield return result;
                }
            }
            else
            {
                depth++;
            }
        }
    }
}