namespace ObservableHashCollections;
public class ReplaceElementObservableHashList<T> : ObservableHashList<T> where T : notnull
{
    public ReplaceElementObservableHashList(IEqualityComparer<T>? selectionKeyEqualityComparer, IEqualityComparer<T>? fullEqualityComparer)
         : base(selectionKeyEqualityComparer, fullEqualityComparer)
    {

    }

    protected override void ReplaceConsecutiveOldElementsWithNew(int index, List<T> newElements)
    {
        RemoveRange(index, newElements.Count);
        InsertRange(index, newElements);
    }

    protected override void ReplaceOldElementWithNew(T oldElement, T newElement)
    {
        oldElement = ValuesBySelectionKey[oldElement];
        var oldElementIndex = Values.IndexOf(oldElement);
        ValuesBySelectionKey[oldElement] = newElement;
        Values[oldElementIndex] = newElement;
    }
}
