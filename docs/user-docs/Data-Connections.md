Data connections are a way to add data sources that can be accessed easily in scripts. Data connections were designed to
support a wide variety of data sources like databases, Excel files, flat files...etc. NetPad currently supports database
connections for these providers:

* Microsoft SQL Server
* PostgreSQL
* SQLite
* MySQL
* MariaDB

:computer_mouse: To add a connection, click the `+` icon in the <kbd>Connections</kbd> explorer pane. Clicking <kbd>
Save</kbd> once you're done will save the connection and scaffold it, making it ready to use in any of your scripts.

:computer_mouse: To use a connection, drag and drop it to a script, or use the <kbd>Connections</kbd> dropdown in the
toolbar to select the connection. Alternatively, you can right-click a connection and select <kbd>Use in Current
script</kbd>.

> :bulb: **Requirement**
> You need to have the `dotnet-ef` tool installed to use this feature:
> ```shell
> dotnet tool install --global dotnet-ef
> ```
> You need version 6 or later.

## Data Context

When a database connection is assigned on a script, NetPad scaffolds the data source using the Entity Framework Core
dotnet tool (`dotnet-ef`) and generates a `DbContext` that can be accessed directly in the script using the
`DataContext` property:

```csharp
DataContext.AspNetRoles.Where(...).Dump();
DataContext.AspNetRoles.Add(...);
DataContext.SaveChanges();
```

You can also access `DbContext` properties and methods directly without going through the `DataContext` property:

```csharp
AspNetRoles.Where(...).Dump();
AspNetRoles.Add(...);
SaveChanges();
```

## Configuring the `DbContext`

Your script program inherits from `DbContext` so you can override and configure the generated `DbContext` as needed:

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

> :bulb: **Note**
>
> Since scripts in NetPad are Top-Level Statements, they run in a static context. That means you cannot directly
> override `DbContext` methods. Instead add the methods you want to override in the partial `Program` class as shown in
> the example above.

## Compiled Models

Compiled models can improve EF Core startup time for applications with large models. A large model typically means
hundreds to thousands of entity types and relationships. You can opt to use a compiled model in the <kbd>
Scaffolding</kbd>
tab of the connection properties dialog.

Compiled models are not effective for smaller models and have some limitations when used. To learn more about compiled
models and their limitations
see [this](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models).

## Schema Caching

After a `DbContext` is generated for a connection, it will be cached for re-use. The validity of the cached `DbContext`
will be verified the first time it is used after starting NetPad. If the schema has changed it will be re-scaffolded and
a new `DbContext` will be generated and cached.

You can manually re-scaffold the database anytime by right-clicking a connection and selecting `Refresh`.

#### How does schema cache validation work?

Since each database engine works a little differently, different strategies are needed to determine if a schema has
changed since the last time we saw it. In general though, the first time a database is scaffolded, metadata specific to
that database's schema will be stored. The next time NetPad is started, and the connection used, database metadata will
be recalculated and compared with the metadata we've previously cached.

If the two values do not match, NetPad will re-scaffold the database and store the new metadata to compare against in
the future. The important part is that generating this metadata is far quicker that scaffolding the database everytime
NetPad is started.

Below is an outline of what metadata is stored, per database engine type. This metadata is compared against to determine
if the schema has changed:

| Engine               | Metadata                                                                                                                                                                                                                                                                                                                                                                |
|:---------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Microsoft SQL Server | The date of the last modified user-created object:<br />`SELECT max(modify_date)`<br />`FROM sys.objects`<br />`WHERE is_ms_shipped = 'False'`<br />&nbsp;&nbsp;&nbsp;`AND type IN ('F', 'PK', 'U', 'V')`                                                                                                                                                               |
| PostgreSQL           | A MD5 hash is calculated from the result of:<br />`SELECT table_schema, table_name, column_name, is_nullable, data_type, is_identity`<br />`FROM information_schema.columns`<br />`WHERE table_schema NOT IN ('pg_catalog', 'information_schema')`<br />&nbsp;&nbsp;&nbsp;`AND table_schema NOT LIKE 'pg_toast%'`<br />`ORDER BY table_schema, table_name, column_name` |
| SQLite               | A MD5 hash is calculated from the result of:<br />`SELECT sql FROM sqlite_schema`                                                                                                                                                                                                                                                                                       |
| MySQL/MariaDB        | A MD5 hash is calculated from the result of:<br />`SELECT table_schema, table_name, column_name, is_nullable, data_type`<br />`FROM information_schema.columns`<br />`WHERE table_schema NOT IN ('mysql', 'performance_schema', information_schema', 'sys')`<br />`ORDER BY table_schema, table_name, column_name`                                                      |
