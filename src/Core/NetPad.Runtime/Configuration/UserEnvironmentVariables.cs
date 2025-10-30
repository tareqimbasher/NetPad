using Microsoft.AspNetCore.DataProtection;
using NetPad.IO;
using NetPad.IO.Stores;

namespace NetPad.Configuration;

// NOTE: currently not used. Will be soon though.

/// <summary>
/// Represents a user-defined environment variable that can be optionally protected using data protection.
/// </summary>
public class UserEnvironmentVariable : IProtectableItem
{
    private string? _value;
    public string Key { get; init; } = string.Empty;

    public string? Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public bool IsProtected { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Provides methods for storing, retrieving, and managing user-defined environment variables.
/// </summary>
public class UserEnvironmentVariables(FilePath storeFilePath, IDataProtector dataProtector)
{
    private readonly JsonKeyValueStore<FileData, UserEnvironmentVariable> _valueStore = new(storeFilePath, dataProtector);

    private class FileData : IKeyedItemsStore<UserEnvironmentVariable>
    {
        public Dictionary<string, UserEnvironmentVariable> Items { get; set; } = new();
    }

    /// <summary>
    /// Lists all stored environment variables.
    /// </summary>
    /// <returns>An array of <see cref="UserEnvironmentVariable"/> objects.</returns>
    public UserEnvironmentVariable[] List() => _valueStore.List();

    /// <summary>
    /// Gets the value of a stored environment variable as a string by its key.
    /// </summary>
    /// <param name="key">The key (name) of the environment variable.</param>
    /// <returns>The unprotected value.</returns>
    /// <exception cref="KeyNotFoundException">If no item with the specified key exists.</exception>
    public string? Get(string key) => Get<string>(key);

    /// <summary>
    /// Gets the value of a stored environment variable and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the variable value into.</typeparam>
    /// <param name="key">The key (name) of the environment variable.</param>
    /// <returns>The unprotected value.</returns>
    /// <exception cref="KeyNotFoundException">If no item with the specified key exists.</exception>
    public T? Get<T>(string key)
    {
        return _valueStore.GetValue<T>(key);
    }

    /// <summary>
    /// Saves or updates an environment variable value.
    /// </summary>
    /// <typeparam name="T">The type of the value being stored.</typeparam>
    /// <param name="key">The key (name) of the environment variable.</param>
    /// <param name="value">The value to save.</param>
    /// <param name="protect">
    /// Whether to protect (encrypt) the value using data protection.
    /// If <c>null</c>, the existing protection setting for the variable is used.
    /// </param>
    public void Save<T>(string key, T value, bool? protect = null)
    {
        _valueStore.Save(key, value, protect);
    }

    /// <summary>
    /// Deletes an environment variable by its key.
    /// </summary>
    /// <param name="key">The key (name) of the environment variable to delete.</param>
    /// <returns><c>true</c> if the variable was deleted; otherwise, <c>false</c>.</returns>
    public bool Delete(string key)
    {
        return _valueStore.Delete(key);
    }

    /// <summary>
    /// Deletes all environment variables that match the specified predicate.
    /// </summary>
    /// <param name="predicate">A predicate used to match environment variables for deletion.</param>
    public void Delete(Func<UserEnvironmentVariable, bool> predicate)
    {
        _valueStore.Delete(predicate);
    }
}
