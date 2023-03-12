using System.Reflection;

namespace ObservableHashCollections;
internal static class RaiseEventExtensions
{
    internal static void Raise<TEventArgs>(this object source, string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
    {
        var sourceType = source.GetType();
        bool propertyChangedInvoked = false;
        do
        {
            if (sourceType.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(source) is MulticastDelegate eventDelegate)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                    handler.Method.Invoke(handler.Target, new[] { source, eventArgs });
                propertyChangedInvoked = true;
            }
            else
            {
                sourceType = sourceType.BaseType;
            }
        } while (!propertyChangedInvoked && sourceType != typeof(object) && sourceType != null);
    }
}