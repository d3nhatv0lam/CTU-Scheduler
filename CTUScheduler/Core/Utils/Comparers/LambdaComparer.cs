using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CTUScheduler.Core.Utils.Comparers;

public class LambdaComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T?, T?, bool> _lambda;
    private readonly Func<T, int> _hashSelector;

    public LambdaComparer(Func<T?, T?, bool> lambda, Func<T, int>? hashSelector = null)
    {
        ArgumentNullException.ThrowIfNull(lambda);
        _lambda = lambda;
        _hashSelector = hashSelector ?? (obj => obj?.GetHashCode() ?? 0);
    }

    public bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return _lambda(x, y);
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _hashSelector(obj);
    }
}