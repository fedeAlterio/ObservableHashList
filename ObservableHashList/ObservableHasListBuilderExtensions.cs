using System.ComponentModel;

namespace ObservableHashCollections;
public static class ObservableHasListBuilderExtensions
{
    #region Public methods

    public static IObservableHashListFullEqualityChooser<T> WithDefaultSelectionKey<T>(this IObservableHashListSelectionKeyChooser<T> @this)
        where T : notnull
    {
        var defaultComparer = GetDefaultComparer<T>();
        return @this.WithSelectionKey(defaultComparer);
    }

    public static IObservableHashListFullEqualityChooser<T> WithSelectionKey<T>(this IObservableHashListSelectionKeyChooser<T> @this, Func<T, object> selectionKey)
        where T : notnull
    {
        var comparer = DelegateEqualityComparer.New(selectionKey);
        return @this.WithSelectionKey(comparer);
    }

    public static IObservableHashListUpdateStrategyChooser<T> WithDefaultEquality<T>(this IObservableHashListFullEqualityChooser<T> @this)
        where T : notnull
    {
        var defaultComparer = GetDefaultComparer<T>();
        return @this.WithEquality(defaultComparer);
    }

    public static IObservableHashListUpdateStrategyChooser<T> CheckEqualityAgainst<T>(this IObservableHashListFullEqualityChooser<T> @this, Func<T, object> equalityMap)
        where T : notnull
    {
        var comparer = DelegateEqualityComparer.New(equalityMap);
        return @this.WithEquality(comparer);
    }

    public static IObservableHashListUpdateStrategyChooser<T> ForEqualityCheckAlso<T>(this IObservableHashListFullEqualityChooser<T> @this, Func<T, object> equalityMap)
        where T : notnull
    {
        var comparer = DelegateEqualityComparer.New(equalityMap);
        return @this.ForEqualityCheckAlso(comparer);
    }

    #endregion

    #region Private methods

    static IEqualityComparer<T> GetDefaultComparer<T>()
    {
        var defaultComparer = EqualityComparer<T>.Default;
        if (typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T)))
            return new IgnorePropertyChangedComprer<T>(defaultComparer);

        return defaultComparer;
    }

    #endregion
}
