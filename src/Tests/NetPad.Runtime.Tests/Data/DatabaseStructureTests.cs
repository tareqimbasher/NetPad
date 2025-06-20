using NetPad.Data.Metadata;
using Xunit;

namespace NetPad.Runtime.Tests.Data;

public class DatabaseStructureTests
{
    [Fact]
    public void DatabaseStructure_DatabaseName()
    {
        var dbName = "Database name";

        var structure = new DatabaseStructure(dbName);

        Assert.Equal(dbName, structure.DatabaseName);
        Assert.Empty(structure.Schemas);
    }

    [Fact]
    public void AddingSchema_ShouldAddNewSchema()
    {
        var structure = new DatabaseStructure("Database name");
        var baseName = "schema ";

        for (int i = 0; i < 3; i++)
        {
            var name = i == 0 ? null : $"{baseName} {i}";
            var schema = structure.GetOrAddSchema(name);

            Assert.Equal(name, schema.Name);
            Assert.Equal(i + 1, structure.Schemas.Count);
            Assert.Equal(name, structure.Schemas[i].Name);
            Assert.Empty(structure.Schemas[i].Tables);
        }
    }

    [Fact]
    public void AddingSameSchema_ShouldNotCreateDuplicateSchema()
    {
        var structure = new DatabaseStructure("Database name");
        var schemaName = "schema name";

        for (int i = 0; i < 2; i++)
        {
            var schema = structure.GetOrAddSchema(schemaName);

            Assert.Equal(schemaName, schema.Name);
            Assert.Single(structure.Schemas);
            Assert.Equal(schemaName, structure.Schemas.Single().Name);
        }
    }

    [Fact]
    public void AddingTable_ShouldAddNewTable()
    {
        var structure = new DatabaseStructure("Database name");
        var schema = structure.GetOrAddSchema("schema name");


        var baseName = "table ";

        for (int i = 0; i < 3; i++)
        {
            var name = $"{baseName} {i}";
            var displayName = $"{baseName} display {i}";
            var table = schema.GetOrAddTable(name, displayName);

            Assert.Equal(name, table.Name);
            Assert.Equal(i + 1, schema.Tables.Count);
            Assert.Equal(name, schema.Tables[i].Name);
            Assert.Empty(table.Columns);
        }
    }

    [Fact]
    public void AddingSameTable_ShouldNotCreateDuplicateTable()
    {
        var structure = new DatabaseStructure("Database name");
        var schema = structure.GetOrAddSchema("schema name");
        var name = "table name";
        var displayName = "table display name";

        for (int i = 0; i < 2; i++)
        {
            var table = schema.GetOrAddTable(name, displayName);

            Assert.Equal(name, table.Name);
            Assert.Equal(displayName, table.DisplayName);
            Assert.Single(structure.Schemas);
            Assert.Equal(name, schema.Tables.Single().Name);
            Assert.Equal(displayName, schema.Tables.Single().DisplayName);
            Assert.Empty(schema.Tables.Single().Columns);
        }
    }

    [Fact]
    public void AddingColumn_ShouldAddNewColumn()
    {
        var structure = new DatabaseStructure("Database name");
        var schema = structure.GetOrAddSchema("schema name");
        var table = schema.GetOrAddTable("table name", "table display name");

        var baseName = "column ";

        for (int i = 0; i < 3; i++)
        {
            var name = $"{baseName} {i}";
            var column = table.GetOrAddColumn(name, "type", "crltype", false, false);

            Assert.Equal(name, column.Name);
            Assert.Equal(i + 1, table.Columns.Count);
            Assert.Equal(name, table.Columns[i].Name);
        }
    }

    [Fact]
    public void AddingSameColumn_ShouldNotCreateDuplicateColumn()
    {
        var structure = new DatabaseStructure("Database name");
        var schema = structure.GetOrAddSchema("schema name");
        var table = schema.GetOrAddTable("table name", "table display name");
        var name = "column name";

        for (int i = 0; i < 2; i++)
        {
            var column = table.GetOrAddColumn(name, "type", "crltype", false, false);

            Assert.Equal(name, column.Name);
            Assert.Single(table.Columns);
            Assert.Equal(name, table.Columns.Single().Name);
        }
    }
}
