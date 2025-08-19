using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CTUScheduler.Core.Algorithms;

public static class Combinatorics
{
    public static IEnumerable<List<T>> CartesianProduct<T>(
        IEnumerable<List<T>> sets,
        Func<List<T>, bool>? isValidPrefix = null,
        Func<List<T>, bool>? isValidFull = null,
        CancellationToken? token = null)
    {
        var setList = sets.ToList();
        
        var indices = new int[setList.Count];
        var current = new List<T>(setList.Count);

        // bắt đầu với index 0
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
                if (current.Count > depth) current.RemoveAt(current.Count - 1);
                continue;
            }

            // chọn phần tử hiện tại
            if (current.Count > depth)
                current[depth] = setList[depth][indices[depth]];
            else
                current.Add(setList[depth][indices[depth]]);

            // kiểm tra prefix
            if (isValidPrefix != null && !isValidPrefix(current))
                continue;

            if (depth == setList.Count - 1)
            {
                // đủ tổ hợp
                if (isValidFull == null || isValidFull(current))
                    yield return new List<T>(current);
            }
            else
            {
                depth++;
            }
        }
    }
}