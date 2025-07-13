# Data Connections

Data connections let scripts tap into data sources and easily query, add and update data. Connections were designed to
support a wide variety of data sources like databases, Excel files, flat files...etc. NetPad currently **only supports**
database connections; the plan is to expand support in the near future.

To access database connections, NetPad uses EntityFramework Core, and supports the following providers:

* Microsoft SQL Server
* PostgreSQL
* SQLite
* MySQL
* MariaDB

:computer_mouse: To add a new connection, click the `+` icon in the <kbd>Connections</kbd> explorer pane. When you're
finished, click <kbd>Save</kbd> to store your settings and auto-generate the scaffolding, making the connection
instantly available in your scripts.

:computer_mouse: To use a connection, drag it and drop it onto a script, or use the <kbd>Connections</kbd> dropdown in
the toolbar to select the connection. You can also right-click a connection and select <kbd>Use in Current script</kbd>.

## Requirements

You need to have the `dotnet-ef` tool installed (version 6 or later) to use database connections:

```shell
dotnet tool install --global dotnet-ef
```

## Usage

When a database connection is created, NetPad scaffolds it using `dotnet-ef` and generates a `DbContext` and gives your
script access to it via the `DataContext` property:

```csharp
DataContext.AspNetRoles.Where(...).Dump();
DataContext.AspNetRoles.Add(...);
DataContext.SaveChanges();
```

For convenience, NetPad allows you to reference certain `DbContext` members (`DbSet<T>` and methods) directly
without going through the `DataContext` property:

```csharp
AspNetRoles.Where(...).Dump();
AspNetRoles.Add(...);
SaveChanges();
```

## Configuring the `DbContext`

Your script program inherits from `DbContext`, so you can override and configure the generated `DbContext` as needed:

```csharp
var role = AspNetRoles.First(...);

role.Name = "NewRoleName";

SaveChanges();

partial class Program
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    // OnConfiguring is a special case. To override OnConfiguring, implement this partial method.
    partial void OnConfiguringPartial(DbContextOptionsBuilder optionsBuilder)
    {
       // Your code
    }
}
```

> :bulb: Since scripts in NetPad are Top-Level Statements, they run in a static context. That means you cannot directly
> override `DbContext` methods. Instead, add the methods you want to override in the `partial Program` class as shown in
> the example above.

## Compiled Models

Compiled models can improve script startup time for connections with large models. A "large model" typically means
hundreds to thousands of entity types and relationships. You can opt to use a compiled model in the <kbd>
Scaffolding</kbd> tab of the connection properties dialog.

Compiled models are not effective for smaller models and have some limitations when used. To learn more about compiled
models and their limitations
see [this](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models).

## Schema Caching

After a `DbContext` is generated for a connection, it is cached for re-use. The validity of the cached `DbContext`
will be verified the first time it is used after starting NetPad. If the schema has changed it will be re-scaffolded and
a new `DbContext` will be generated and cached.

> :bulb: You can manually re-scaffold the `DbContext` anytime by right-clicking a connection and selecting <kbd>
> Refresh</kbd>.

#### How does NetPad figure out if cache needs to be invalidated?

Since each database provider operates differently, NetPad uses specific strategies to detect if the schema has changed
since it was cached. Generally, when a database is scaffolded for the first time, schema-specific metadata is
stored. Whenever NetPad restarts and the connection is used again, it recalculates the database metadata and compares it
to the previously cached version.

If differences are found, NetPad re-scaffolds the database and updates the stored metadata for future comparisons. This
approach is significantly faster than re-scaffolding the database every time NetPad starts.

Below is an outline of what metadata is stored and used for comparison, per database provider:

| Database             | Metadata                                                                                                                                                                                                                                                                                                                                                                |
|:---------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Microsoft SQL Server | The date of the last modified user-created object:<br />`SELECT max(modify_date)`<br />`FROM sys.objects`<br />`WHERE is_ms_shipped = 'False'`<br />&nbsp;&nbsp;&nbsp;`AND type IN ('F', 'PK', 'U', 'V')`                                                                                                                                                               |
| PostgreSQL           | A MD5 hash is calculated from the result of:<br />`SELECT table_schema, table_name, column_name, is_nullable, data_type, is_identity`<br />`FROM information_schema.columns`<br />`WHERE table_schema NOT IN ('pg_catalog', 'information_schema')`<br />&nbsp;&nbsp;&nbsp;`AND table_schema NOT LIKE 'pg_toast%'`<br />`ORDER BY table_schema, table_name, column_name` |
| SQLite               | A MD5 hash is calculated from the result of:<br />`SELECT sql FROM sqlite_schema`                                                                                                                                                                                                                                                                                       |
| MySQL/MariaDB        | A MD5 hash is calculated from the result of:<br />`SELECT table_schema, table_name, column_name, is_nullable, data_type`<br />`FROM information_schema.columns`<br />`WHERE table_schema NOT IN ('mysql', 'performance_schema', information_schema', 'sys')`<br />`ORDER BY table_schema, table_name, column_name`                                                      |
