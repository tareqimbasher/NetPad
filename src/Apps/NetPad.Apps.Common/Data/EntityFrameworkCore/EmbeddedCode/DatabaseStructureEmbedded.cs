using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;

// Since this code is embedded we don't want to use newer language features
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UseCollectionExpression

// ReSharper disable once CheckNamespace
namespace NetPad.Embedded;

internal static class EntityFrameworkDatabaseUtil
{
    public static DatabaseStructure GetDatabaseStructure(DbContext dbContext)
    {
        var dbSets = dbContext.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => new
            {
                p.Name,
                ElementType = p.PropertyType.GenericTypeArguments.First()
            }).ToDictionary(k => k.ElementType, v => v.Name);

        var structure = new DatabaseStructure(dbContext.Database.GetDbConnection().Database);

        var model = dbContext.GetService<IDesignTimeModel>().Model;

        var defaultSchema = model.GetDefaultSchema();

        foreach (var entityType in model.GetEntityTypes())
        {
            var schema = structure.GetOrAddSchema(entityType.GetSchema() ?? entityType.GetDefaultSchema() ?? defaultSchema);

            var tableName = entityType.GetTableName() ?? entityType.Name;
            dbSets.TryGetValue(entityType.ClrType, out string? dbSetName);
            var table = schema.GetOrAddTable(tableName, dbSetName ?? tableName);

            foreach (var index in entityType.GetIndexes())
            {
                table.AddIndex(
                    index.GetDatabaseName() ?? index.DisplayName(),
                    index.IsUnique,
                    index.Properties.Select(p => p.Name).ToArray());
            }

            foreach (var navigation in entityType.GetNavigations())
            {
                var targetEntityType = navigation.TargetEntityType;
                table.AddNavigation(
                    navigation.Name,
                    targetEntityType.GetTableName() ?? targetEntityType.Name,
                    navigation.PropertyInfo?.PropertyType.Name);
            }

            foreach (var property in entityType.GetProperties())
            {
                var columnType = property.GetColumnType();
                var precision = property.GetScale();
                var scale = property.GetScale();

                if (precision != null || scale != null)
                {
                    columnType += $" ({precision},{scale})";
                }

                if (property.IsNullable)
                {
                    columnType += " (nullable)";
                }

                var column = table.GetOrAddColumn(
                    property.Name,
                    columnType,
                    property.ClrType.Name,
                    property.IsPrimaryKey(),
                    property.IsForeignKey());

                column.SetOrder(property.GetColumnOrder());
            }
        }

        return structure;
    }

    public class DatabaseStructure
    {
        private readonly List<DatabaseSchema> _schemas = new();

        public DatabaseStructure(string databaseName)
        {
            DatabaseName = databaseName;
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
        private readonly List<DatabaseTable> _tables = new();

        public DatabaseSchema(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; }
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

    public class DatabaseTable
    {
        private readonly List<DatabaseTableColumn> _columns = new();
        private readonly List<DatabaseIndex> _indexes = new();
        private readonly List<DatabaseTableNavigation> _navigations = new();

        public DatabaseTable(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        public string Name { get; }
        public string DisplayName { get; }
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
        public DatabaseTableNavigation(string name, string target, string? clrType)
        {
            Name = name;
            Target = target;
            ClrType = clrType;
        }

        public string Name { get; }
        public string Target { get; }
        public string? ClrType { get; }
    }

    public class DatabaseIndex
    {
        public DatabaseIndex(string name, bool isUnique, string[] columns)
        {
            Name = name;
            IsUnique = isUnique;
            Columns = columns;
        }

        public string Name { get; }
        public bool IsUnique { get; }
        public string[] Columns { get; }
    }
}
