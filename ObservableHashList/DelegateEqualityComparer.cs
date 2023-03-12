using System;
using System.Collections;

namespace ObservableHashCollections;
public class DelegateEqualityComparer : IEqualityComparer
{
    readonly DelegateEqualityComparer<object> _equalityComparer;

    public static DelegateEqualityComparer<T> New<T>(Func<T, object> selector)
    {
        return new DelegateEqualityComparer<T>(selector);
    }

    public DelegateEqualityComparer(Func<object, object> selector)
    {
        _equalityComparer = New(selector);
    }

    public new bool Equals(object x, object y)
    {
        return _equalityComparer.Equals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return _equalityComparer.GetHashCode();
    }
}

public class DelegateEqualityComparer<T> : IEqualityComparer<T>
{
    readonly Func<T, object> _selctor;

    public DelegateEqualityComparer(Func<T, object> selector)
    {
        _selctor = selector;
    }

    public bool Equals(T x, T y)
    {
        var (valueX, valueY) = (_selctor.Invoke(x), _selctor.Invoke(y));
        return Equals(valueX, valueY);
    }

    public int GetHashCode(T obj)
    {
        return _selctor.Invoke(obj).GetHashCode();
    }

    public DelegateEqualityComparer OrDefaultComparerForOthers()
        => new DelegateEqualityComparer(obj => obj is T t ? _selctor(t) : obj);
}
