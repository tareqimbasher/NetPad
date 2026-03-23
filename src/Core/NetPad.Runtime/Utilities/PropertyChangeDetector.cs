using System.Reflection;

namespace NetPad.Utilities;

public static class PropertyChangeDetector
{
    /// <summary>
    /// Compares public instance properties of two objects, skipping properties
    /// whose names are in the exclusion set. Returns true if any non-excluded property changed.
    /// </summary>
    public static bool HasChanges<T>(T existing, T updated, HashSet<string> excludedProperties) where T : class
    {
        var properties = typeof(T) == updated.GetType()
            ? typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            : updated.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (excludedProperties.Contains(property.Name))
                continue;

            var existingValue = property.GetValue(existing);
            var updatedValue = property.GetValue(updated);

            if (existingValue is null && updatedValue is null)
                continue;

            if (existingValue is null || updatedValue is null || !existingValue.Equals(updatedValue))
                return true;
        }

        return false;
    }
}
