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

        // depth : dộ sâu, hay Row
        // indices[depth]: ở depth thì đang chọn phần tử thứ mấy
        // current : path hiện tại
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
                continue;
            }
            
            // dọn dữ liệu thừa ở các nhánh trước
            if (current.Count > depth)
            {
                current.RemoveRange(depth, current.Count - depth);
            }

            T candidate = setList[depth][indices[depth]];

            if (isValidCandidate is not null && !isValidCandidate(current, candidate))
            {
                continue;
            }
            
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
        ArgumentNullException.ThrowIfNull(sets);
        if (sets.Length == 0) yield break;
        int count = sets.Length;
        for (int i = 0; i < count; i++)
        {
            ArgumentNullException.ThrowIfNull(sets[i]);
            if (sets[i].Length == 0) yield break;
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
    
    // C# 9+ modern
    public delegate bool CandidateValidator<T>(ReadOnlySpan<T> current, T candidate, int depth);
    public delegate bool FullValidator<T>(ReadOnlySpan<T> current);
    public static IEnumerable<T[]> CartesianProduct<T>(
        IReadOnlyList<IReadOnlyList<T>> sets,
        CandidateValidator<T>? isValidCandidate = null,
        FullValidator<T>? isValidFull = null,
        CancellationToken token = default)
    {
        if (sets.Count == 0) yield break;

        // Early validation
        for (int i = 0; i < sets.Count; i++)
        {
            if (sets[i].Count == 0)
                yield break;
        }

        var indices = new int[sets.Count];
        var buffer = new T[sets.Count];
        Array.Fill(indices, -1);

        int depth = 0;

        while (depth >= 0)
        {
            if (token.IsCancellationRequested)
                yield break;

            indices[depth]++;

            if (indices[depth] >= sets[depth].Count)
            {
                indices[depth] = -1;
                depth--;
                continue;
            }

            T candidate = sets[depth][indices[depth]];

            if (isValidCandidate is not null)
            {
                var span = new ReadOnlySpan<T>(buffer, 0, depth);
                if (!isValidCandidate(span, candidate, depth))
                    continue;
            }

            buffer[depth] = candidate;

            if (depth == sets.Count - 1)
            {
                if (isValidFull is null || isValidFull(new ReadOnlySpan<T>(buffer, 0, sets.Count)))
                {
                    var result = new T[sets.Count];
                    Array.Copy(buffer, result, sets.Count);
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