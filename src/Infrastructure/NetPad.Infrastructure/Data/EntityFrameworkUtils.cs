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

        foreach (var entityType in model.GetEntityTypes())
        {
            var schema = structure.GetOrAddSchema(entityType.GetSchema());
            var table = schema.GetOrAddTable(entityType.DisplayName());

            foreach (var property in entityType.GetProperties())
            {
                var columnType = property.GetColumnType();
                var precision = property.GetScale();
                var scale = property.GetScale();

                if (precision != null || scale != null)
                {
                    columnType += $" ({precision},{scale})";
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
