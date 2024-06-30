using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore;

internal static class EntityFrameworkUtils
{
    public static bool IsEntityFrameworkDataConnection(
        this DataConnection dataConnection,
        [MaybeNullWhen(false)] out EntityFrameworkDatabaseConnection entityFrameworkDatabaseConnection)
    {
        if (dataConnection is EntityFrameworkDatabaseConnection ef)
        {
            entityFrameworkDatabaseConnection = ef;
            return true;
        }

        entityFrameworkDatabaseConnection = null;
        return false;
    }

    public static DatabaseStructure GetDatabaseStructure(this DbContext dbContext)
    {
        var dbSets = dbContext.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsOfGenericType(typeof(DbSet<>)))
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
                    index.GetDatabaseName(),
                    index.GetMethod(),
                    index.IsUnique,
                    index.IsClustered() ?? false,
                    index.Properties.Select(p => p.Name).ToArray());
            }

            foreach (var navigation in entityType.GetNavigations())
            {
                var targetEntityType = navigation.TargetEntityType;
                table.AddNavigation(
                    navigation.Name,
                    targetEntityType.GetTableName() ?? targetEntityType.Name,
                    navigation.PropertyInfo?.PropertyType.GetReadableName());
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
                    property.ClrType.GetReadableName(),
                    property.IsPrimaryKey(),
                    property.IsForeignKey());

                column.SetOrder(property.GetColumnOrder());
            }
        }

        return structure;
    }
}
