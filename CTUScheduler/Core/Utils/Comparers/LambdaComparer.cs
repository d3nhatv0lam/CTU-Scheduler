using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CTUScheduler.Core.Utils.Comparers;

public class LambdaComparer<T>(Func<T, T, bool> lambda) : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        return lambda(x, y);
    }
    public int GetHashCode([DisallowNull] T obj) => 0;
}