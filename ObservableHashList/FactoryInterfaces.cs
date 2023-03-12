namespace ObservableHashCollections;
public interface IObservableHashListSelectionKeyChooser<T> where T : notnull
{
    #region Public methods

    IObservableHashListFullEqualityChooser<T> WithSelectionKey(IEqualityComparer<T> selectionKeyEqualityComparer);

    #endregion
}

public interface IObservableHashListFullEqualityChooser<T> where T : notnull
{
    #region Public methods

    IObservableHashListUpdateStrategyChooser<T> WithEquality(IEqualityComparer<T> fullEqualityComparer);
    IObservableHashListUpdateStrategyChooser<T> ForEqualityCheckAlso(IEqualityComparer<T> fullEqualityComparer);

    #endregion
}

public interface IObservableHashListUpdateStrategyChooser<T> where T : notnull
{
    DeepCopyObservableHashList<T> OnUpdateDeepCopy();
    ReplaceElementObservableHashList<T> OnUpdateReplaceItem();
}

