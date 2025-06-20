using System.Text.Json.Serialization;

namespace NetPad.Data.Metadata;

public class DatabaseStructure(string databaseName)
{
    private List<DatabaseSchema> _schemas = [];

    [JsonInclude] public string DatabaseName { get; private set; } = databaseName;

    [JsonInclude]
    public IReadOnlyList<DatabaseSchema> Schemas
    {
        get => _schemas;
        private set => _schemas = (List<DatabaseSchema>)value;
    }

    public DatabaseSchema GetOrAddSchema(string? name)
    {
        var schema = _schemas.FirstOrDefault(s => s.Name == name);
        if (schema != null)
        {
            return schema;
        }

        schema = new DatabaseSchema(name);
        _schemas.Add(schema);
        return schema;
    }
}

public class DatabaseSchema(string? name = null)
{
    private List<DatabaseTable> _tables = [];

    [JsonInclude] public string? Name { get; private set; } = name;

    [JsonInclude]
    public IReadOnlyList<DatabaseTable> Tables
    {
        get => _tables;
        private set => _tables = (List<DatabaseTable>)value;
    }

    public DatabaseTable GetOrAddTable(string name, string displayName)
    {
        var table = _tables.FirstOrDefault(t => t.Name == name);
        if (table != null)
        {
            return table;
        }

        table = new DatabaseTable(name, displayName);
        _tables.Add(table);
        return table;
    }
}

public class DatabaseTable(string name, string displayName)
{
    private List<DatabaseTableColumn> _columns = [];
    private List<DatabaseIndex> _indexes = [];
    private List<DatabaseTableNavigation> _navigations = [];

    [JsonInclude] public string Name { get; private set; } = name;
    [JsonInclude] public string DisplayName { get; private set; } = displayName;

    [JsonInclude]
    public IReadOnlyList<DatabaseTableColumn> Columns
    {
        get => _columns;
        private set => _columns = (List<DatabaseTableColumn>)value;
    }

    [JsonInclude]
    public IReadOnlyList<DatabaseIndex> Indexes
    {
        get => _indexes;
        private set => _indexes = (List<DatabaseIndex>)value;
    }

    [JsonInclude]
    public IReadOnlyList<DatabaseTableNavigation> Navigations
    {
        get => _navigations;
        private set => _navigations = (List<DatabaseTableNavigation>)value;
    }

    public DatabaseTableColumn GetOrAddColumn(string name, string type, string clrType, bool isPrimaryKey, bool isForeignKey)
    {
        var column = _columns.FirstOrDefault(t => t.Name == name);
        if (column != null)
        {
            return column;
        }

        column = new DatabaseTableColumn(name, type, clrType, isPrimaryKey, isForeignKey);
        _columns.Add(column);
        return column;
    }

    public DatabaseIndex AddIndex(string name, bool isUnique, string[] columns)
    {
        var index = new DatabaseIndex(name, isUnique, columns);
        _indexes.Add(index);
        return index;
    }

    public DatabaseTableNavigation AddNavigation(string name, string target, string? clrType)
    {
        var navigation = new DatabaseTableNavigation(name, target, clrType);
        _navigations.Add(navigation);
        return navigation;
    }
}

public class DatabaseTableColumn(string name, string type, string clrType, bool isPrimaryKey, bool isForeignKey)
{
    [JsonInclude] public string Name { get; private set; } = name;
    [JsonInclude] public string Type { get; private set; } = type;
    [JsonInclude] public string ClrType { get; private set; } = clrType;
    [JsonInclude] public bool IsPrimaryKey { get; private set; } = isPrimaryKey;
    [JsonInclude] public bool IsForeignKey { get; private set; } = isForeignKey;
    [JsonInclude] public int? Order { get; private set; }

    public void SetOrder(int? order)
    {
        if (order < 0)
            throw new ArgumentException("Order cannot be less than 0");

        Order = order;
    }
}

public class DatabaseTableNavigation(string name, string target, string? clrType)
{
    [JsonInclude] public string Name { get; private set; } = name;
    [JsonInclude] public string Target { get; private set; } = target;
    [JsonInclude] public string? ClrType { get; private set; } = clrType;
}

public class DatabaseIndex(string name, bool isUnique, string[] columns)
{
    [JsonInclude] public string Name { get; private set; } = name;
    [JsonInclude] public bool IsUnique { get; private set; } = isUnique;
    [JsonInclude] public string[] Columns { get; private set; } = columns;
}
