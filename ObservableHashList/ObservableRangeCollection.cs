using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableHashCollections;
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    const string COUNT_STRING = "Count";
    const string INDEXER_NAME = "Item[]";

    public void AddRange(IEnumerable<T> items)
    {
        CheckReentrancy();

        var itemsList = items as List<T> ?? items.ToList();
        var startingIndex = Count;
        if (Items is List<T> list)
        {
            list.AddRange(itemsList);
        }
        else
        {
            var newIndex = Items.Count;
            foreach (var item in itemsList)
                Items.Insert(newIndex++, item);
        }

        if (itemsList.Count == 0)
            return;

        OnPropertyChanged(COUNT_STRING);
        OnPropertyChanged(INDEXER_NAME);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems: itemsList, startingIndex: startingIndex));
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        CheckReentrancy();

        var itemsList = items as List<T> ?? items.ToList();

        if (Items is List<T> list)
        {
            list.InsertRange(index, itemsList);
        }
        else
        {
            var newIndex = index;
            foreach (var item in itemsList)
                Items.Insert(newIndex++, item);
        }

        if (itemsList.Count == 0)
            return;

        OnPropertyChanged(COUNT_STRING);
        OnPropertyChanged(INDEXER_NAME);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems: itemsList, startingIndex: index));
    }


    public void RemoveRange(int index, int count)
    {
        if (count == 1)
        {
            RemoveAt(index);
            return;
        }

        CheckReentrancy();

        List<T> removedItems;
        if (Items is List<T> list)
        {
            removedItems = list.GetRange(index, count);
            list.RemoveRange(index, count);
        }
        else
        {
            removedItems = new List<T>(count);
            for (var i = 0; i < count; i++)
            {
                removedItems.Add(this[index]);
                Items.RemoveAt(index);
            }
        }

        if (removedItems.Count == 0)
            return;

        OnPropertyChanged(COUNT_STRING);
        OnPropertyChanged(INDEXER_NAME);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItems: removedItems, startingIndex: index));
    }

    void OnPropertyChanged(string propertyName)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
}