using Microsoft.AspNetCore.DataProtection;
using NetPad.IO;
using NetPad.IO.Stores;

namespace NetPad.Configuration;

/// <summary>
/// Represents a user secret, such as an API key or password, which is stored securely.
/// </summary>
public class UserSecret : IProtectableItem
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

    public bool IsProtected
    {
        get => true;
        set { }
    }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Provides methods for storing and retrieving user secrets.
/// </summary>
public class UserSecrets(FilePath storeFilePath, IDataProtector dataProtector)
{
    private readonly JsonKeyValueStore<FileData, UserSecret> _valueStore = new(storeFilePath, dataProtector);

    private class FileData : IKeyedItemsStore<UserSecret>
    {
        public Dictionary<string, UserSecret> Items { get; set; } = new();
    }

    /// <summary>
    /// Lists all stored user secrets.
    /// </summary>
    /// <returns>An array of <see cref="UserSecret"/> objects.</returns>
    public UserSecret[] List() => _valueStore.List();

    /// <summary>
    /// Gets a user secret by key.
    /// </summary>
    /// <param name="key">The key of the secret.</param>
    /// <returns>A <see cref="UserSecret"/>, or null if not found.</returns>
    public UserSecret? GetSecret(string key) => _valueStore.Get(key);

    /// <summary>
    /// Gets a secret value by its key.
    /// </summary>
    /// <param name="key">The key of the secret.</param>
    /// <returns>The unprotected secret value</returns>
    /// <exception cref="KeyNotFoundException">If no item with the specified key exists.</exception>
    public string Get(string key) => _valueStore.GetValue<string>(key) ?? string.Empty;

    /// <summary>
    /// Saves or updates a secret value.
    /// </summary>
    /// <param name="key">The key of the secret.</param>
    /// <param name="value">The value to save.</param>
    public void Save(string key, string value)
    {
        if (value == null!)
        {
            throw new ArgumentNullException(nameof(value), "Secret values cannot be null");
        }

        _valueStore.Save(key, value, true);
    }

    /// <summary>
    /// Deletes a secret by its key.
    /// </summary>
    /// <param name="key">The key of the secret to delete.</param>
    /// <returns><c>true</c> if the secret was deleted; otherwise, <c>false</c>.</returns>
    public bool Delete(string key)
    {
        return _valueStore.Delete(key);
    }

    /// <summary>
    /// Deletes all secrets that match the specified predicate.
    /// </summary>
    /// <param name="filter">A predicate used to match secrets for deletion.</param>
    public void Delete(Func<UserSecret, bool> filter)
    {
        _valueStore.Delete(filter);
    }
}
