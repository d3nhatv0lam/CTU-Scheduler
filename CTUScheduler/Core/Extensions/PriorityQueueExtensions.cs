using System;
using System.Collections.Generic;
using System.Linq;

namespace CTUScheduler.Core.Extensions;

public static class PriorityQueueExtensions
{
    /// <summary>
    /// Xuất danh sách theo thứ tự ưu tiên tăng dần (Nhỏ đến Lớn - mặc định của Min-Heap).
    /// </summary>
    public static IReadOnlyList<TElement> ToOrderList<TElement, TPriority>(this PriorityQueue<TElement, TPriority> queue,
        int? take = null)
    {
        var copy = new PriorityQueue<TElement, TPriority>(queue.UnorderedItems);
        var count = Math.Min(copy.Count, take ?? copy.Count);

        var array = new TElement[count];

        for (var i = 0; i < count; i++)
        {
            array[i] = copy.Dequeue();
        }

        return array.ToList();
    }

    /// <summary>
    /// Xuất danh sách theo thứ tự ưu tiên giảm dần (Lớn đến Nhỏ).
    /// </summary>
    public static IReadOnlyList<TElement> ToReverseOrderList<TElement, TPriority>(this PriorityQueue<TElement, TPriority> queue,
        int? take = null)
    {
        var copy = new PriorityQueue<TElement, TPriority>(queue.UnorderedItems);
        var count = Math.Min(copy.Count, take ?? copy.Count);

        if (count <= 0) return [];

        var array = new TElement[count];

        for (var i = count - 1; i >= 0; i--)
        {
            array[i] = copy.Dequeue();
        }

        return array.ToList();
    }
}