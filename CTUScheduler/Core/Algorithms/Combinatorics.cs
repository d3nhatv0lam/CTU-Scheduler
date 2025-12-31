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
        Func<IReadOnlyList<T>, bool>? isValidPrefix = null,
        Func<IReadOnlyList<T>, bool>? isValidFull = null,
        CancellationToken? token = null)
    {
        var setList = sets as IReadOnlyList<IReadOnlyList<T>> ?? sets.ToList();
        if (setList.Count == 0 || setList.Any(s => s.Count == 0))
            yield break;

        var indices = new int[setList.Count];
        var current = new List<T>(setList.Count);

        int depth = 0;
        for (int i = 0; i < setList.Count; i++) indices[i] = -1;

        while (depth >= 0)
        {
            if (token?.IsCancellationRequested == true)
                yield break;

            indices[depth]++;

            if (indices[depth] >= setList[depth].Count)
            {
                indices[depth] = -1;
                depth--;

                if (depth >= 0 && current.Count > depth) current.RemoveAt(current.Count - 1);
                continue;
            }

            // chọn phần tử hiện tại
            if (current.Count > depth)
                current[depth] = setList[depth][indices[depth]];
            else
                current.Add(setList[depth][indices[depth]]);

            // kiểm tra prefix
            if (isValidPrefix is not null && !isValidPrefix(current))
                continue;

            if (depth == setList.Count - 1)
            {
                if (isValidFull == null || isValidFull(current))
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
        Func<T[], int, bool>? isValidPrefix = null, // int = T[].size
        Func<T[], bool>? isValidFull = null, // T[].size = sets.size
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

            // đỡ viết sets[depth][..]
            T[] currentSet = sets[depth];

            indices[depth]++;

            if (indices[depth] >= currentSet.Length)
            {
                indices[depth] = -1;
                depth--;
                continue;
            }

            // currentSet[indices[depth]] <=> sets[depth][indices[depth]]
            currentBuffer[depth] = currentSet[indices[depth]];

            // --- VALIDATE PREFIX ---
            if (isValidPrefix != null)
            {
                if (!isValidPrefix(currentBuffer, depth + 1))
                    continue;
            }

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