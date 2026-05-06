using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CTUScheduler.Core.Algorithms;

public static class Combinatorics
{
    /// <summary>
    /// Delegate dùng để cắt tỉa (prune) các nhánh không hợp lệ ngay trong quá trình sinh.
    /// Sử dụng ReadOnlySpan để đảm bảo Zero-Allocation (không tạo mảng mới/không rác GC).
    /// </summary>
    public delegate bool CandidateValidator<T>(ReadOnlySpan<T> current, T candidate);

    /// <summary>
    /// Delegate kiểm tra điều kiện của toàn bộ tập hợp sau khi đã đi đến độ sâu cuối cùng.
    /// </summary>
    public delegate bool FullValidator<T>(ReadOnlySpan<T> fullPath);

    /// <summary>
    /// Sinh tích Đề-các (Cartesian Product) cho danh sách các tập hợp.
    /// <br/>
    /// O(∏(sets[i].size)) 
    /// </summary>
    public static IEnumerable<T[]> CartesianProduct<T>(
        IReadOnlyList<IReadOnlyList<T>> sets,
        CandidateValidator<T>? isValidCandidate = null,
        FullValidator<T>? isValidFull = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(sets);

        if (sets.Count == 0 || sets.Any(s => s == null || s.Count == 0))
            yield break;

        var indices = new int[sets.Count];
        var buffer = new T[sets.Count];

        Array.Fill(indices, -1);
        int depth = 0;

        while (depth >= 0)
        {
            if (token.IsCancellationRequested)
                yield break;

            indices[depth]++;

            // Hết lựa chọn ở depth hiện tại -> quay lui
            if (indices[depth] >= sets[depth].Count)
            {
                indices[depth] = -1;
                depth--;
                continue;
            }

            T candidate = sets[depth][indices[depth]];

            if (isValidCandidate is not null)
            {
                if (!isValidCandidate(buffer.AsSpan(0, depth), candidate))
                    continue;
            }

            buffer[depth] = candidate;

            if (depth == sets.Count - 1)
            {
                if (isValidFull is null || isValidFull(buffer.AsSpan()))
                {
                    yield return (T[])buffer.Clone();
                }
            }
            else
            {
                depth++;
            }
        }
    }
}