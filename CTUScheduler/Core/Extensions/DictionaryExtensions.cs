using System.Collections.Generic;

namespace CTUScheduler.Core.Extensions;

public static class DictionaryExtensions
{
    public static bool DictionaryEquals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> d1,
        IReadOnlyDictionary<TKey, TValue> d2)
    {
        if (ReferenceEquals(d1,d2)) return true;
        if (d1 is null || d2 is null) return false;
        if (d1.Count != d2.Count) return false;
        
        foreach (var kvp in d1)
        {
            if (!d2.TryGetValue(kvp.Key, out var value))
                return false;

            if (!EqualityComparer<TValue>.Default.Equals(kvp.Value, value))
                return false;
        }
        return true;
    }
}