using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NetPad.Common;

public interface INotifyOnPropertyChanged
{
    List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }
}

public class PropertyChangedArgs
{
    public PropertyChangedArgs(object obj, string propertyName, object? oldValue, object? newValue)
    {
        Object = obj;
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public object Object { get; }
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}

public static class INotifyOnPropertyChangedExtensions
{
    public static TReturn RaiseAndSetIfChanged<TObject, TReturn>(
        this TObject obj,
        ref TReturn backingField,
        TReturn newValue,
        [CallerMemberName] string? propertyName = null)
        where TObject : INotifyOnPropertyChanged
    {
        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        if (EqualityComparer<TReturn>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        TReturn oldValue = backingField;

        backingField = newValue;
        foreach (var handler in obj.OnPropertyChanged)
        {
            AsyncUtil.RunSync(() => handler(new PropertyChangedArgs(obj, propertyName, oldValue, newValue)));
        }

        return newValue;
    }

    public static void RemoveAllPropertyChangedHandlers<TObject>(this TObject obj) where TObject : INotifyOnPropertyChanged
    {
        obj.OnPropertyChanged?.Clear();
    }
}
