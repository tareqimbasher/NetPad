namespace NetPad.Data;

public class ConnectionStringBuilder() : Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase)
{
    public ConnectionStringBuilder(string connectionString) : this()
    {
        var keyValues = connectionString
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Contains('=', StringComparison.InvariantCulture))
            .Select(kv => kv.Split('=', StringSplitOptions.TrimEntries))
            .GroupBy(kv => kv[0])
            .Select(g => g.Last())
            .ToArray();

        var dict = keyValues.ToDictionary(
            x => x[0],
            x => x.Length > 1 ? string.Join('=', x.Skip(1)) : null,
            StringComparer.InvariantCultureIgnoreCase);

        foreach (var kv in dict)
        {
            Add(kv.Key, kv.Value);
        }
    }

    public void Augment(Dictionary<string, string?> augmentation)
    {
        foreach (var augmentedKey in augmentation.Keys)
        {
            this[augmentedKey] = augmentation[augmentedKey];
        }
    }

    public string Build() => ToString();

    public override string ToString()
    {
        return string.Join(";", this.Select(x => $"{x.Key}={x.Value}")) + ";";
    }

    public static ConnectionStringBuilder Parse(string connectionString) => new (connectionString);
}
