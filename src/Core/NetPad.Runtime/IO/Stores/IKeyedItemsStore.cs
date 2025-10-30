namespace NetPad.IO.Stores;

/// <summary>
/// A collection of keyed items that can be stored and retrieved by key.
/// </summary>
/// <typeparam name="TItem">
/// The type of items in the store. Must implement <see cref="IKeyedItem"/>.
/// </typeparam>
public interface IKeyedItemsStore<TItem> where TItem : class, IKeyedItem
{
    Dictionary<string, TItem> Items { get; set; }
}

public interface IKeyedItem
{
    /// <summary>
    /// Gets or sets the unique key identifying this item.
    /// </summary>
    public string Key { get; init; }
    /// <summary>
    /// Gets or sets the value of this item.
    /// </summary>
    public string? Value { get; set; }
    /// <summary>
    /// Gets or sets the UTC date and time when this item was last updated.
    /// </summary>
    DateTime UpdatedAtUtc { get; set; }
}

public interface IProtectableItem : IKeyedItem
{
    /// <summary>
    /// Gets a value indicating whether this item is protected by data protection.
    /// </summary>
    bool IsProtected { get; set; }
}
