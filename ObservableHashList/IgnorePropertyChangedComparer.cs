using System.ComponentModel;
using System.Reflection;

namespace ObservableHashCollections;
internal class IgnorePropertyChangedComprer<T> : IEqualityComparer<T> where T : notnull
{
    #region Fields

    readonly FieldInfo _propertyChanged;
    readonly IEqualityComparer<T> _equalityComparer;

    #endregion

    #region Public methods

    public bool Equals(T? x, T? y)
    {
        using (new ForceNullOnPropertyChanged(y, _propertyChanged))
        using (new ForceNullOnPropertyChanged(x, _propertyChanged))
            return _equalityComparer.Equals(x, y);
    }

    public int GetHashCode(T x)
    {
        using (new ForceNullOnPropertyChanged(x, _propertyChanged))
            return _equalityComparer.GetHashCode(x);
    }

    #endregion

    #region Constructor

    public IgnorePropertyChangedComprer(IEqualityComparer<T> equalityComparer)
    {
        FieldInfo GetProeprtyChanged()
        {
            var type = typeof(T);
            do
            {
                var field = type.GetField(nameof(INotifyPropertyChanged.PropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    return field;

                type = type.BaseType;
            } while (type != typeof(object) && type != null);

            throw new InvalidOperationException("Expected a INotifyPropertyChanged instance");
        }

        _propertyChanged = GetProeprtyChanged();
        _equalityComparer = equalityComparer;
    }

    #endregion

    readonly struct ForceNullOnPropertyChanged : IDisposable
    {
        readonly object? _propertyChangedBackup;
        readonly FieldInfo? _propertyChanged;
        readonly T? _x;

        public ForceNullOnPropertyChanged(T? x, FieldInfo propertyChanged)
        {
            if (x == null)
            {
                _propertyChanged = null;
                _propertyChangedBackup = null;
                _x = default;
            }

            _x = x;
            _propertyChanged = propertyChanged;
            _propertyChangedBackup = propertyChanged.GetValue(x);
            propertyChanged.SetValue(x, null);
        }

        public void Dispose()
        {
            if (_propertyChangedBackup == null)
                return;

            _propertyChanged?.SetValue(_x, _propertyChangedBackup);
        }
    }
}

