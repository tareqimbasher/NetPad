namespace NetPad.Data;

public class DatabaseStructure(string databaseName)
{
    private readonly List<DatabaseSchema> _schemas = [];

    public string DatabaseName { get; } = databaseName;
    public IReadOnlyList<DatabaseSchema> Schemas => _schemas;

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
    private readonly List<DatabaseTable> _tables = [];

    public string? Name { get; } = name;
    public IReadOnlyList<DatabaseTable> Tables => _tables;

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
    private readonly List<DatabaseTableColumn> _columns = [];
    private readonly List<DatabaseIndex> _indexes = [];
    private readonly List<DatabaseTableNavigation> _navigations = [];

    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public IReadOnlyList<DatabaseTableColumn> Columns => _columns;
    public IReadOnlyList<DatabaseIndex> Indexes => _indexes;
    public IReadOnlyList<DatabaseTableNavigation> Navigations => _navigations;

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

    public DatabaseIndex AddIndex(string name, string? type, bool isUnique, bool isClustered, string[] columns)
    {
        var index = new DatabaseIndex(name, type, isUnique, isClustered, columns);
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
    public string Name { get; } = name;
    public string Type { get; } = type;
    public string ClrType { get; } = clrType;
    public bool IsPrimaryKey { get; } = isPrimaryKey;
    public bool IsForeignKey { get; } = isForeignKey;
    public int? Order { get; private set; }

    public void SetOrder(int? order)
    {
        if (order < 0)
            throw new ArgumentException("Order cannot be less than 0");

        Order = order;
    }
}

public class DatabaseTableNavigation(string name, string target, string? clrType)
{
    public string Name { get; } = name;
    public string Target { get; } = target;
    public string? ClrType { get; } = clrType;
}

public class DatabaseIndex(string name, string? type, bool isUnique, bool isClustered, string[] columns)
{
    public string Name { get; } = name;
    public string? Type { get; } = type;
    public bool IsUnique { get; } = isUnique;
    public bool IsClustered { get; } = isClustered;
    public string[] Columns { get; } = columns;
}
