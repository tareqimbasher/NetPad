using System;
using System.Collections.Generic;
using System.Linq;
using NetPad.Utilities;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding.Transforms;

public class AnyDatabaseProviderTransform : IScaffoldedModelTransform
{
    public void Transform(ScaffoldedDatabaseModel model)
    {
        PatchDbContextCode(model.DbContextFile);
    }

    private static void PatchDbContextCode(ScaffoldedSourceFile dbContextFile)
    {
        // The issue is that EF Core doesn't generate the "entity.ToTable()" statement for
        // some tables/entities in the OnModelCreating() method. As a result, changing the name
        // that EF Core gives the DbSet property will cause an error since it seemingly relies on
        // that name to get the table name, unless a "entity.ToTable()" statement maps the DbSet to the
        // proper table name. Here we explicitly add the "entity.ToTable()" statement when it doesn't
        // already exist.
        if (dbContextFile.Code == null) return;
        var lines = dbContextFile.Code.Split(Environment.NewLine).ToList();
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

        dbContextFile.SetCode(lines.JoinToString(Environment.NewLine));
    }
}
