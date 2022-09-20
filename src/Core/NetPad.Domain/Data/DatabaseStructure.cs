using System;
using System.Collections.Generic;
using System.Linq;

namespace NetPad.Data;

public class DatabaseStructure
{
    private readonly List<DatabaseSchema> _schemas;

    public DatabaseStructure(string databaseName)
    {
        DatabaseName = databaseName;
        _schemas = new List<DatabaseSchema>();
    }

    public string DatabaseName { get; }
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

public class DatabaseSchema
{
    private readonly List<DatabaseTable> _tables;

    public DatabaseSchema(string? name = null)
    {
        _tables = new List<DatabaseTable>();
        Name = name;
    }

    public string? Name { get; }
    public IReadOnlyList<DatabaseTable> Tables => _tables;

    public DatabaseTable GetOrAddTable(string name)
    {
        var table = _tables.FirstOrDefault(t => t.Name == name);
        if (table != null)
        {
            return table;
        }

        table = new DatabaseTable(name);
        _tables.Add(table);
        return table;
    }
}

public class DatabaseTable
{
    private readonly List<DatabaseTableColumn> _columns;
    private readonly List<DatabaseIndex> _indexes;
    private readonly List<DatabaseTableNavigation> _navigations;

    public DatabaseTable(string name)
    {
        _columns = new List<DatabaseTableColumn>();
        _indexes = new List<DatabaseIndex>();
        _navigations = new List<DatabaseTableNavigation>();
        Name = name;
    }

    public string Name { get; }
    public IReadOnlyList<DatabaseTableColumn> Columns => _columns;
    public IReadOnlyList<DatabaseIndex> Indexes => _indexes;
    public IReadOnlyList<DatabaseTableNavigation> Navigations => _navigations;

    public DatabaseTableColumn AddColumn(string name, string type, string clrType, bool isPrimaryKey, bool isForeignKey)
    {
        if (_columns.Any(t => t.Name == name))
            throw new InvalidOperationException($"A column named '{name}' already exists in table '{Name}'");

        var column = new DatabaseTableColumn(name, type, clrType, isPrimaryKey, isForeignKey);
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

public class DatabaseTableColumn
{
    public DatabaseTableColumn(string name, string type, string clrType, bool isPrimaryKey, bool isForeignKey)
    {
        Name = name;
        Type = type;
        ClrType = clrType;
        IsPrimaryKey = isPrimaryKey;
        IsForeignKey = isForeignKey;
    }

    public string Name { get; }
    public string Type { get; }
    public string ClrType { get; }
    public bool IsPrimaryKey { get; }
    public bool IsForeignKey { get; }
    public int? Order { get; private set; }

    public void SetOrder(int? order)
    {
        if (order < 0)
            throw new ArgumentException("Order cannot be less than 0");

        Order = order;
    }
}

public class DatabaseTableNavigation
{
    public string Name { get; }
    public string Target { get; }
    public string? ClrType { get; }

    public DatabaseTableNavigation(string name, string target, string? clrType)
    {
        Name = name;
        Target = target;
        ClrType = clrType;
    }
}

public class DatabaseIndex
{
    public string Name { get; }
    public string? Type { get; }
    public bool IsUnique { get; }
    public bool IsClustered { get; }
    public string[] Columns { get; }

    public DatabaseIndex(string name, string? type, bool isUnique, bool isClustered, string[] columns)
    {
        Name = name;
        Type = type;
        IsUnique = isUnique;
        IsClustered = isClustered;
        Columns = columns;
    }
}
