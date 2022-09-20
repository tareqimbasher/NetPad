using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NetPad.Utilities;

namespace NetPad.Data;

internal static class EntityFrameworkUtils
{
    public static DatabaseStructure GetDatabaseStructure(this DbContext dbContext)
    {
        var structure = new DatabaseStructure(dbContext.Database.GetDbConnection().Database);

        var model = dbContext.GetService<IDesignTimeModel>().Model;

        var defaultSchema = model.GetDefaultSchema();

        foreach (var entityType in model.GetEntityTypes())
        {
            var schema = structure.GetOrAddSchema(entityType.GetSchema() ?? entityType.GetDefaultSchema() ?? defaultSchema);

            var table = schema.GetOrAddTable(entityType.GetTableName() ?? entityType.Name);

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
                table.AddNavigation(navigation.Name, targetEntityType.GetTableName() ?? targetEntityType.Name, navigation.PropertyInfo?.PropertyType.GetReadableName());
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

                var column = table.AddColumn(
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
