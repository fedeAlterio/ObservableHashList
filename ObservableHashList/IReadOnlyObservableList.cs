using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableHashCollections;
public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{

}