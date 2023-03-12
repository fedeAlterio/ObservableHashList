using System.ComponentModel;
using System.Reflection;

namespace ObservableHashCollections;
public class DeepCopyObservableHashList<T> : ObservableHashList<T> where T : notnull
{
    static readonly Assembly _systemAssembly = typeof(object).Assembly;

    public bool AutoInvokeNotifyPropertyChanged { get; set; } = true;

    public DeepCopyObservableHashList(IEqualityComparer<T>? selectionKeyEqualityComparer = null, IEqualityComparer<T>? fullEqualityComparer = null)
        : base(selectionKeyEqualityComparer, fullEqualityComparer)
    {

    }


    protected override void ReplaceConsecutiveOldElementsWithNew(int index, List<T> newElements)
    {
        foreach (var newElement in newElements)
        {
            ReplaceOldElementWithNew(ValuesBySelectionKey[newElement], newElement);
        }
    }

    protected override void ReplaceOldElementWithNew(T oldElement, T newElement)
    {
        object? toUpdateObject = oldElement;
        DeepCopyProperties(newElement, ref toUpdateObject);
    }

    void DeepCopyProperties(object mapFrom, ref object? mapTo)
    {
        if (mapFrom is null || mapTo is null)
        {
            mapTo = mapFrom;
            return;
        }

        var type = mapFrom.GetType();
        if (type != mapTo.GetType())
            throw new InvalidOperationException("It's allowed only to map an object into itself");

        if (IsSystemType(type))
        {
            mapTo = mapFrom;
            return;
        }

        var mapToInstance = mapTo;
        var propertyInfo = from property in mapFrom.GetType().GetProperties()
                           select new
                           {
                               Property = property,
                               MapFromValue = property.GetValue(mapFrom),
                               MapToValue = property.GetValue(mapToInstance)
                           };

        foreach (var info in propertyInfo)
        {
            var propertyType = info.Property.PropertyType;

            if (ShouldMapObservableHashCollection())
            {
                MapObservableHashCollection();
            }
            else if (PropertyHasSetter())
            {
                DeepCopyProperty();
            }

            #region HelpersFunctions

            bool ShouldMapObservableHashCollection()
            {
                return propertyType.IsGenericType
                       && propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(ObservableHashList<>))
                       && info.MapToValue != null;
            }

            void MapObservableHashCollection()
            {
                var collectionType = propertyType;
                var collectionGenericParameter = collectionType.GetGenericArguments()[0];

                object mapToCollection = info.MapToValue;
                if (mapToCollection is null)
                    return;

                if (info.MapFromValue is null)
                {
                    var method = mapToCollection.GetType().GetMethod(nameof(Clear));
                    method?.Invoke(mapToCollection, null);
                }
                else
                {
                    var method = mapToCollection.GetType().GetMethod(nameof(Refresh));
                    var items = Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionGenericParameter));
                    method?.Invoke(mapToCollection, new[] { items });
                }
            }

            bool PropertyHasSetter() => info.Property.GetSetMethod() != null;

            void DeepCopyProperty()
            {
                var mapToValue = info.MapToValue;
                DeepCopyProperties(info.MapFromValue, ref mapToValue);

                info.Property.SetValue(mapToInstance, mapToValue);
                if (AutoInvokeNotifyPropertyChanged && mapToInstance is INotifyPropertyChanged)
                    mapToInstance.Raise(nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(info.Property.Name));
            }

            #endregion
        }

        static bool IsSystemType(Type type)
        {
            Assembly typeAssembly = type.Assembly;
            return typeAssembly == _systemAssembly;
        }
    }
}
