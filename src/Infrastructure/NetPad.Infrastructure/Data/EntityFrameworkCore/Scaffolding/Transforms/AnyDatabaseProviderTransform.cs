using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding.Transforms;

public class AnyDatabaseProviderTransform : IScaffoldedModelTransform
{
    public void Transform(ScaffoldedDatabaseModel model)
    {
        AddAndUseGenericDbContext(model);
        EnsureTableMappingsForAllEntities(model);
    }

    private void AddAndUseGenericDbContext(ScaffoldedDatabaseModel model)
    {
        var dbContextFile = model.DbContextFile;
        if (dbContextFile.Code.Value == null) return;

        var sb = new StringBuilder(dbContextFile.Code.Value);

        // Convert the existing DbContext to a generic class
        sb.Replace(
            $"partial class {dbContextFile.ClassName} : DbContext",
            $"partial class {dbContextFile.ClassName}<TContext> : DbContext where TContext : DbContext");

        sb.Replace(
            $"DbContextOptions<{dbContextFile.ClassName}>",
            "DbContextOptions<TContext>");

        // Add a non-generic DbContext that inherits from the new generic DbContext
        sb.AppendLine($@"
public partial class {dbContextFile.ClassName} : {dbContextFile.ClassName}<{dbContextFile.ClassName}>
{{
    public {dbContextFile.ClassName}()
    {{
    }}

    public {dbContextFile.ClassName}(DbContextOptions<{dbContextFile.ClassName}> options)
        : base(options)
    {{
    }}
}}
");

        // If a compiled model was generated, make generic modification on compiled model
        var compiledModel = model.DbContextCompiledModelFile;
        if (compiledModel?.Code.Value != null)
        {
            compiledModel.Code.Update(compiledModel.Code.Value.Replace(
                $"[DbContext(typeof({EntityFrameworkDatabaseScaffolder.DbContextName}))]",
                $"[DbContext(typeof({EntityFrameworkDatabaseScaffolder.DbContextName}<>))]"));
        }

        dbContextFile.Code.Update(sb.ToString());
    }

    private static void EnsureTableMappingsForAllEntities(ScaffoldedDatabaseModel model)
    {
        var dbContextFile = model.DbContextFile;
        if (dbContextFile.Code.Value == null) return;

        // The issue is that EF Core doesn't generate the "entity.ToTable()" statement for
        // some tables/entities in the OnModelCreating() method. As a result, changing the name
        // that EF Core gives the DbSet property will cause an error since it seemingly relies on
        // that name to get the table name, unless a "entity.ToTable()" statement maps the DbSet to the
        // proper table name. Here we explicitly add the "entity.ToTable()" statement when it doesn't
        // already exist.
        var lines = dbContextFile.Code.Value.Split(Environment.NewLine).ToList();
        var entityNameToDbSetName = new Dictionary<string, string>();

        for (int iLine = 0; iLine < lines.Count; iLine++)
        {
            var line = lines[iLine];

            string entityName;
            string dbSetName;

            if (line.Contains("public virtual DbSet<"))
            {
                // This is a DbSet property. Get the name of the DbSet property
                entityName = line.SubstringBetween("<", ">").Trim();
                dbSetName = line.SubstringBetween("> ", " {").Trim();

                entityNameToDbSetName.Add(entityName, dbSetName);

                continue;
            }

            if (!line.Contains("modelBuilder.Entity<")) continue;
            // We are configuring an entity's model

            var iLineWithToTableStatement = iLine + 2;
            var lineWithToTableStatement = lines[iLineWithToTableStatement];

            if (lineWithToTableStatement.Contains("entity.ToTable(")) continue;
            // No explicit "ToTable()" mapping exists, add it

            entityName = line.SubstringBetween("<", ">").Trim();
            dbSetName = entityNameToDbSetName[entityName];

            lines.Insert(iLineWithToTableStatement, $"entity.ToTable(\"{dbSetName}\");");
            iLine += 2;
        }

        dbContextFile.Code.Update(lines.JoinToString(Environment.NewLine));
    }
}
