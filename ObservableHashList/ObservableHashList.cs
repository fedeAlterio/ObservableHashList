using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableHashCollections;
public abstract class ObservableHashList<T> : IReadOnlyObservableList<T>, IList<T>, IList where T : notnull
{
    #region Fields

    protected readonly ObservableRangeCollection<T> Values = new ObservableRangeCollection<T>();
    protected readonly Dictionary<T, T> ValuesBySelectionKey;
    readonly Dictionary<T, int> _itemsIndexByItem;
    int? _startIndexCacheOutOfDate = 0;
    public readonly IEqualityComparer<T> SelectionKeyEqualityComparer;
    public readonly IEqualityComparer<T> FullEqualityComparer;

    #endregion

    #region Properties

    public int Count => Values.Count;
    public bool IsReadOnly => ((ICollection<T>)Values).IsReadOnly;
    public bool IsFixedSize => false;
    public object SyncRoot => this;
    public bool IsSynchronized => false;
    public T this[int index]
    {
        get => Values[index];
        set => ReplaceOldElementWithNew(this[index], value);
    }

    #endregion

    #region Initilization

    /// <summary>
    /// Consider using ObservableHashCollection.New
    /// </summary>
    protected ObservableHashList(IEqualityComparer<T>? primaryKeyEqualityComparer = default, IEqualityComparer<T>? fullEqualityComparer = default)        
    {
        if (fullEqualityComparer == null)
            fullEqualityComparer = EqualityComparer<T>.Default;

        if (primaryKeyEqualityComparer == null)
            primaryKeyEqualityComparer = EqualityComparer<T>.Default;

        ValuesBySelectionKey = new Dictionary<T, T>(primaryKeyEqualityComparer);
        Values.CollectionChanged += (_, args) => CollectionChanged?.Invoke(this, args);
        _itemsIndexByItem = new Dictionary<T, int>(primaryKeyEqualityComparer);
        ((INotifyPropertyChanged)Values).PropertyChanged += (_, args) => PropertyChanged?.Invoke(this, args);
        SelectionKeyEqualityComparer = primaryKeyEqualityComparer;
        FullEqualityComparer = fullEqualityComparer;
    }

    #endregion

    #region Events

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Public

    public void Refresh(IList<T> elements)
    {
        // elements already contained in the collection (according to selection key)
        var toUpdateElementsInfo = from x in elements.Select((element, newIndex) => new { newIndex, element })
                                   let index = IndexOf(x.element)
                                   where index != -1
                                   select new
                                   {
                                       NewIndex = x.newIndex,
                                       NewElement = x.element,
                                   };

        var toUpdateElementIndicesByElement = toUpdateElementsInfo
            .ToDictionary(x => x.NewElement, x => x.NewIndex, SelectionKeyEqualityComparer);

        var updatedElements = toUpdateElementIndicesByElement.Keys;
        RemoveAllItemsBut(updatedElements);
        AddAllElementsBut(elements, elementsToNotAdd: updatedElements);
        UpdateRangeWithNoChecks(updatedElements);
        ReorderElements(toUpdateElementIndicesByElement);
    }

    public void Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var existsAnItemWithSameSelectionKey = ValuesBySelectionKey.ContainsKey(item);
        if (existsAnItemWithSameSelectionKey)
            throw NewElementWithSameSelectionKeyException(item);

        AddWihoutChecks(item);
    }

    public void AddRange(IEnumerable<T> elements)
    {
        var elementsList = elements as IList<T> ?? elements.ToList();
        AddRange(elementsList);
    }

    public void AddRange(IList<T> elements)
    {
        foreach (var element in elements)
            if (Contains(element))
                throw NewElementWithSameSelectionKeyException(element);

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            AddItemToSelectionKeyCache(element);
            _itemsIndexByItem[element] = Count + i;
        }

        Values.AddRange(elements);
    }

    public void AddOrUpdate(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        if (ValuesBySelectionKey.TryGetValue(item, out var toUpdateItem))
            ReplaceOldElementWithNewIfDifferent(item, toUpdateItem);
        else
            AddWihoutChecks(item);
    }

    public void UpdateRange(IEnumerable<T> items)
    {
        IEnumerable<T> ItemsWithCheck()
        {
            foreach (var item in items)
            {
                if (!Contains(item))
                    throw NewElementWithSameSelectionKeyException(item);

                yield return item;
            }
        }

        UpdateRangeWithNoChecks(ItemsWithCheck());
    }


    void UpdateRangeWithNoChecks(IEnumerable<T> items)
    {
        int startChunkIndex = -1;
        int endChunkIndex = -1;
        List<T>? itemsChunk = new List<T>();

        void UpdateChunk()
        {
            ReplaceConsecutiveOldElementsWithNew(startChunkIndex, itemsChunk);

            startChunkIndex = -1;
            itemsChunk = null;
            itemsChunk = new List<T>();
        }

        foreach (var updatedItem in items)
        {
            var toUpdateItemIndex = IndexOf(updatedItem);
            var toUpdateItem = this[toUpdateItemIndex];
            var isEqual = FullEqualityComparer.Equals(updatedItem, toUpdateItem);

            if (isEqual)
                continue;

            var isChunkChanged = endChunkIndex != -1 && endChunkIndex + 1 != toUpdateItemIndex;
            if (isChunkChanged)
                UpdateChunk();

            itemsChunk.Add(updatedItem);
            endChunkIndex = toUpdateItemIndex;
            if (startChunkIndex == -1)
                startChunkIndex = endChunkIndex;
        }

        if (itemsChunk.Count > 0)
            UpdateChunk();
    }

    public void Update(T item)
    {      
        if (!ValuesBySelectionKey.TryGetValue(item, out var toUpdateItem))
            throw NewElementWithSameSelectionKeyException(item);

        ReplaceOldElementWithNewIfDifferent(item, toUpdateItem);
    }


    public void Clear()
    {
        _startIndexCacheOutOfDate = null;
        _itemsIndexByItem.Clear();
        ValuesBySelectionKey.Clear();
        Values.Clear();
    }

    public bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        return ValuesBySelectionKey.ContainsKey(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Values.CopyTo(array, arrayIndex);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Values.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        RemoveAtWithNoChecks(index);
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var index = IndexOf(item);
        if (index == -1)
            return false;

        RemoveAtWithNoChecks(index);
        return true;
    }

    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    public int IndexOf(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (!ValuesBySelectionKey.ContainsKey(item))
            return -1;

        return IndexOfPrivate(item);
    }

    public void Insert(int index, T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (index < 0 || index > Values.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (Contains(item))
            throw NewElementWithSameSelectionKeyException(item);

        AddItemToSelectionKeyCache(item);
        _itemsIndexByItem[item] = index;
        UpdateStartIndexCacheOutOfDate(index + 1);
        Values.Insert(index, item);
    }

    public void InsertRange(int index, IList<T> items)
    {
        if (index < 0 || index > Values.Count)
            throw new ArgumentOutOfRangeException($"{nameof(index)} should be greater or requals to 0 less that the collection count");

        foreach (var item in items)
            if (Contains(item))
                throw new InvalidOperationException($"Element {item} already contained in the collection");

        InsertRangeWithNoChecks(index, items);
    }

    public void RemoveRange(int index, int count)
    {
        if (index < 0 || index + count > Values.Count)
            throw new ArgumentOutOfRangeException($"{nameof(index)} should be greater or requals to 0 and {nameof(index)} + {nameof(count)} should be less that the collection count");

        var maxIndex = index + count;
        for (var i = index; i < maxIndex; i++)
            RemoveCachedInfoForIndex(i);

        Values.RemoveRange(index, count);
    }

    public void Move(int oldIndex, int newIndex)
    {
        UpdateStartIndexCacheOutOfDate(Math.Min(oldIndex, newIndex));
        Values.Move(oldIndex, newIndex);
    }

    public void CopyTo(Array array, int index)
    {
        ((IList)Values).CopyTo(array, index);
    }

    #endregion

    #region Protected

    protected abstract void ReplaceConsecutiveOldElementsWithNew(int index, List<T> newElements);
    protected abstract void ReplaceOldElementWithNew(T oldElement, T newElement);

    #endregion

    #region Explicit Interfaces Implementations

    object? IList.this[int index]
    {
        get => this[index];
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!IsCompatibleObject(value))
                ThrowNotCompatibleObjectException(nameof(value));

            this[index] = (T)value;
        }
    }

    int IList.Add(object? value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (!IsCompatibleObject(value))
            ThrowNotCompatibleObjectException(nameof(value));

        Add((T)value);
        return Count - 1;
    }

    bool IList.Contains(object? value)
    {
        if (IsCompatibleObject(value))
            return Contains((T)value!);

        return false;
    }

    int IList.IndexOf(object? value)
    {
        if (IsCompatibleObject(value))
        {
            return IndexOf((T)value!);
        }
        return -1;
    }

    void IList.Insert(int index, object? value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (!IsCompatibleObject(value))
            ThrowNotCompatibleObjectException(nameof(value));

        Insert(index, (T)value);
    }

    void IList.Remove(object? value)
    {
        if (IsCompatibleObject(value))
            Remove((T)value!);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    #endregion

    #region Private methods

    void UpdateStartIndexCacheOutOfDate(int startIndexOutOfDate)
    {
        if (startIndexOutOfDate <= 0)
        {
            _startIndexCacheOutOfDate = 0;
            return;
        }

        if (_startIndexCacheOutOfDate is null)
        {
            _startIndexCacheOutOfDate = startIndexOutOfDate;
            return;
        }

        _startIndexCacheOutOfDate = Math.Min(startIndexOutOfDate, _startIndexCacheOutOfDate.Value);
    }

    bool ReplaceOldElementWithNewIfDifferent(T newElement, T oldElement)
    {
        if (FullEqualityComparer.Equals(newElement, oldElement))
            return false;

        ReplaceOldElementWithNew(oldElement, newElement);
        return true;
    }

    void ThrowNotCompatibleObjectException(string parameterName) => throw new ArgumentException($"{parameterName} is not a valid {typeof(T)}");

    int GetCachedIndexOrMinus1(T item) => _itemsIndexByItem.TryGetValue(item, out var cachedIndex) ? cachedIndex : -1;

    void AddWihoutChecks(T item)
    {
        AddItemToSelectionKeyCache(item);
        _itemsIndexByItem[item] = Values.Count;
        Values.Add(item);
    }

    bool RemoveFromHashedValues(T item)
    {
        return ValuesBySelectionKey.Remove(item);
    }

    static bool IsCompatibleObject(object? value)
    {
        return value is T || (value == null && default(T) == null);
    }

    void RemoveAllItemsBut(ICollection<T> itemsToNotDelete)
    {
        int lastIndexToRemove = -1;
        int firstIndexToRemove = -1;

        void RemoveConsecutiveBlock()
        {
            if (lastIndexToRemove == -1)
                return;

            RemoveRange(firstIndexToRemove, lastIndexToRemove - firstIndexToRemove + 1);
            lastIndexToRemove = -1;
        }


        for (var i = Values.Count - 1; i >= 0; i--)
        {
            var currentElement = Values[i];
            var isItemContained = itemsToNotDelete.Contains(currentElement);
            if (isItemContained)
            {
                RemoveConsecutiveBlock();
            }
            else
            {
                firstIndexToRemove = i;
                if (lastIndexToRemove == -1)
                    lastIndexToRemove = firstIndexToRemove;
            }
        }

        RemoveConsecutiveBlock();
    }

    void AddAllElementsBut(IList<T> elements, ICollection<T> elementsToNotAdd)
    {
        var firstToAddIndex = -1;
        List<T>? itemsToAdd = null;

        void InsertConsecutiveBlock()
        {
            if (itemsToAdd == null)
                return;

            InsertRangeWithNoChecks(firstToAddIndex, itemsToAdd);
            itemsToAdd = null;
        }

        // Every element in the elements list that is not in the to-update list should be added
        for (var i = 0; i < elements.Count; i++)
        {
            var currentElement = elements[i];
            var shouldAddItem = !elementsToNotAdd.Contains(currentElement);
            if (shouldAddItem)
            {
                if (itemsToAdd is null)
                {
                    itemsToAdd = new List<T>();
                    firstToAddIndex = i;
                }

                itemsToAdd.Add(elements[i]);
            }
            else
            {
                InsertConsecutiveBlock();
            }
        }

        InsertConsecutiveBlock();
    }

    void InsertRangeWithNoChecks(int index, IList<T> items)
    {
        foreach (var item in items)
            AddItemToSelectionKeyCache(item);

        UpdateStartIndexCacheOutOfDate(index);
        Values.InsertRange(index, items);
    }

    bool RemoveAtWithNoChecks(int index)
    {
        if (!RemoveCachedInfoForIndex(index))
            return false;

        Values.RemoveAt(index);
        return true;
    }

    bool RemoveCachedInfoForIndex(int index)
    {
        var item = Values[index];
        _itemsIndexByItem.Remove(item);
        if (!RemoveFromHashedValues(item))
            return false;

        UpdateStartIndexCacheOutOfDate(index);
        return true;
    }


    void AddItemToSelectionKeyCache(T item)
    {
        ValuesBySelectionKey.Add(item, item);
    }

    void ReorderElements(Dictionary<T, int> toReorderElementIndicesByElement)
    {
        // We have to guarantee the correct order of the updated items:
        // - Only the updated items can be out of order, because the added ones have been added in their correct final position
        // - We can't just "force" the correct position, because the notification of the collection change would not be raised.
        //   So we are allowed to call only the Move method.
        // Idea: Let A be an element in the updated list:
        // - 1) We call Move(Pos(A), CorrectIndex) to set A to the correct position
        // - 2) We call Move(CorrectIndex + 1, Pos(A)). A is now in the correct position
        // - 3) Iterating we obtain the correct order

        // Cache to save the positions of all the updated elements
        var currentToUpdateIndixesByItem = toReorderElementIndicesByElement.Keys.ToDictionary(item => item, IndexOf);
        using (var enumerator = toReorderElementIndicesByElement.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                return;

            var current = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var currentElementToMove = current.Key;
                var currentElementToMoveIndex = currentToUpdateIndixesByItem[currentElementToMove];
                var currentElementToMoveCorrectIndex = current.Value;

                if (currentElementToMoveIndex != currentElementToMoveCorrectIndex)
                {
                    // We swap 2 elements, in order to set currentElementToMove to its current position
                    Move(currentElementToMoveIndex, currentElementToMoveCorrectIndex);
                    Move(currentElementToMoveCorrectIndex + 1, currentElementToMoveIndex);

                    // We swap also the indexes (The currentElementToMove position will never be asked again so could be skipped)
                    // currentToUpdateIndixesByItem[currentElementToMove] = currentElementToMoveCorrectIndex;
                    currentToUpdateIndixesByItem[Values[currentElementToMoveIndex]] = currentElementToMoveIndex;
                }

                current = enumerator.Current;
            }
        }
    }

    int IndexOfPrivate(T item)
    {
        // Search if the item exists in the indices cache
        var cachedIndex = GetCachedIndexOrMinus1(item);
        var foundAnIndexInCache = cachedIndex != -1;


        if (_startIndexCacheOutOfDate is null && !foundAnIndexInCache)
            return -1;

        // In case the index is found in cache there are 2 possibilities
        // - The item points to the right index
        // - The item points to the wrong index.
        // The last case implies the cached index is >= _startIndexCacheOutOfDate

        if (foundAnIndexInCache && (_startIndexCacheOutOfDate is null || _startIndexCacheOutOfDate > cachedIndex))
        {
            var realItem = Values[cachedIndex];
            if (SelectionKeyEqualityComparer.Equals(item, realItem))
                return cachedIndex;
        }


        int indexToReturn = -1;
        // If we are here, the item index is necessarly >= _startIndexCacheOutOfDate
        // So we start to search from there and we update the index cache
        
        for (var i = _startIndexCacheOutOfDate!.Value; i < Values.Count; i++)
        {
            var realItem = Values[i];
            _itemsIndexByItem[realItem] = i;
            if (SelectionKeyEqualityComparer.Equals(item, realItem))
                indexToReturn = i;

            _startIndexCacheOutOfDate++;
        }

        if (_startIndexCacheOutOfDate == Values.Count)
            _startIndexCacheOutOfDate = null; // Cache up to date

        return indexToReturn;
    }

    ArgumentException NewElementWithSameSelectionKeyException(T element) => new ArgumentException("An element with the same key already exists", $"{element}");

    #endregion
}


public static class ObservableHashList
{
    #region Public methods

    public static IObservableHashListSelectionKeyChooser<T> New<T>() where T : notnull => new ObservableHashListBuilder<T>();

    #endregion

    class ObservableHashListBuilder<T> :
        IObservableHashListFullEqualityChooser<T>,
        IObservableHashListSelectionKeyChooser<T>,
        IObservableHashListUpdateStrategyChooser<T>
        where T : notnull
    {
        #region Fields

        IEqualityComparer<T>? _selectionKeyEqualityComparer;
        IEqualityComparer<T>? _fullEqualityComparer;

        #endregion

        #region Public methods

        public IObservableHashListUpdateStrategyChooser<T> ForEqualityCheckAlso(IEqualityComparer<T> fullEqualityComparer)
        {
            var equalityComparer = new SumEqualityComparer<T>(_selectionKeyEqualityComparer!, fullEqualityComparer);
            return WithEquality(equalityComparer);
        }

        public DeepCopyObservableHashList<T> OnUpdateDeepCopy()
        {
            return new DeepCopyObservableHashList<T>(_selectionKeyEqualityComparer, _fullEqualityComparer);
        }

        public ReplaceElementObservableHashList<T> OnUpdateReplaceItem()
        {
            return new ReplaceElementObservableHashList<T>(_selectionKeyEqualityComparer, _fullEqualityComparer);
        }

        public IObservableHashListUpdateStrategyChooser<T> WithEquality(IEqualityComparer<T> fullEqualityComparer)
        {
            _fullEqualityComparer = fullEqualityComparer;
            return this;
        }

        public IObservableHashListFullEqualityChooser<T> WithSelectionKey(IEqualityComparer<T> selectionKeyEqualityComparer)
        {
            _selectionKeyEqualityComparer = selectionKeyEqualityComparer;
            return this;
        }

        #endregion
    }

    class SumEqualityComparer<T> : IEqualityComparer<T> where T : notnull
    {
        #region Fields

        readonly IEqualityComparer<T> _first;
        readonly IEqualityComparer<T> _second;

        #endregion

        #region Public methods

        public bool Equals(T? x, T? y)
        {
            return _first.Equals(x, y) && _second.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return (_first.GetHashCode(obj), _second.GetHashCode(obj)).GetHashCode();
        }

        #endregion

        #region Constructor

        public SumEqualityComparer(IEqualityComparer<T> first, IEqualityComparer<T> second)
        {
            _first = first;
            _second = second;
        }

        #endregion
    }
}