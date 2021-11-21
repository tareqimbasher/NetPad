using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NetPad.Utilities;

namespace NetPad.Common
{
    public interface INotifyOnPropertyChanged
    {
        List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }
    }

    public class PropertyChangedArgs
    {
        public PropertyChangedArgs(object obj, string propertyName, object? newValue)
        {
            Object = obj;
            PropertyName = propertyName;
            NewValue = newValue;
        }

        public object Object { get; set; }
        public string PropertyName { get; set; }
        public object? NewValue { get; set; }
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

            backingField = newValue;
            foreach (var handler in obj.OnPropertyChanged)
            {
                AsyncHelpers.RunSync(() => handler(new PropertyChangedArgs(obj, propertyName, newValue)));
            }
            return newValue;
        }

        public static void RemoveAllPropertyChangedHandlers<TObject>(this TObject obj) where TObject : INotifyOnPropertyChanged
        {
            obj.OnPropertyChanged?.Clear();
        }
    }
}
