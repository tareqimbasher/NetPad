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

    public DatabaseTable(string name)
    {
        _columns = new List<DatabaseTableColumn>();
        Name = name;
    }

    public string Name { get; }
    public IReadOnlyList<DatabaseTableColumn> Columns => _columns;

    public DatabaseTableColumn AddColumn(string name, string type, string clrType, bool isPrimaryKey)
    {
        if (_columns.Any(t => t.Name == name))
            throw new InvalidOperationException($"A column named '{name}' already exists in table '{Name}'");

        var column = new DatabaseTableColumn(name, type, clrType, isPrimaryKey);
        _columns.Add(column);
        return column;
    }
}

public class DatabaseTableColumn
{
    public DatabaseTableColumn(string name, string type, string clrType, bool isPrimaryKey)
    {
        Name = name;
        Type = type;
        ClrType = clrType;
        IsPrimaryKey = isPrimaryKey;
    }

    public string Name { get; }
    public string Type { get; }
    public string ClrType { get; }
    public bool IsPrimaryKey { get; }
    public int? Order { get; private set; }

    public void SetOrder(int? order)
    {
        if (order < 0)
            throw new ArgumentException("Order cannot be less than 0");

        Order = order;
    }
}
