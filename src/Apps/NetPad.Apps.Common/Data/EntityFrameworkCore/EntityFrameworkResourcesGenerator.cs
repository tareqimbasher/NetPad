using System.Text;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Security;
using NetPad.DotNet;
using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Apps.Data.EntityFrameworkCore;

internal class EntityFrameworkResourcesGenerator(
    IDataConnectionPasswordProtector dataConnectionPasswordProtector,
    IDotNetInfo dotNetInfo,
    Settings settings,
    ILoggerFactory loggerFactory)
    : IDataConnectionResourcesGenerator
{
    public async Task<DataConnectionResources> GenerateResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection efDbConnection)
        {
            return new DataConnectionResources(dataConnection, DateTime.UtcNow);
        }

        var scaffolder = new EntityFrameworkDatabaseScaffolder(
            dataConnectionPasswordProtector,
            dotNetInfo,
            settings,
            loggerFactory.CreateLogger<EntityFrameworkDatabaseScaffolder>());

        var result = await scaffolder.ScaffoldConnectionResourcesAsync(efDbConnection, targetFrameworkVersion);

        var applicationCode = GenerateApplicationCode(efDbConnection, result.Model.DbContextFile.ClassName, result.Model.DbContextFile.Code.ToCodeString());

        var sourceCode = new DataConnectionSourceCode
        {
            ApplicationCode = applicationCode
        };

        var requiredReferences = EntityFrameworkPackageUtils.GetRequiredReferences(efDbConnection, targetFrameworkVersion);

        return new DataConnectionResources(efDbConnection, DateTime.UtcNow)
        {
            SourceCode = sourceCode,
            Assembly = result.Assembly,
            RequiredReferences = requiredReferences,
            DatabaseStructure = result.DatabaseStructure
        };
    }

    private static SourceCodeCollection GenerateApplicationCode(EntityFrameworkDatabaseConnection efDbConnection, string dbContextClassName,
        string dbContextCode)
    {
        // We want to add utility code to a partial Program class that can be used to augment the Program class in scripts.
        // The goal is to accomplish the following items, mainly for convenience while writing scripts:
        // 1. Make the Program class inherit the generated DbContext
        //    Why? - This allows users to override methods on the base DbContext, ex: the OnConfiguring(DbContextOptionsBuilder optionsBuilder) method.
        //
        // 2. Add a property for the generated DbContext
        //    Why? - This allows users to access the DbContext instance being used in the script
        //
        // 3. Add properties for all the generated DbSet's
        //    Why? - This makes it easy for users to just type in the name of the table/DbSet (ex: "Authors") in their query
        //           instead of having to do something like "DbContext.Authors"

        var code = new StringBuilder();

        // 1. Make the Program class inherit the generated DbContext
        // The program class here will merge with the Program class generated
        // in our Script top-level program
        code.AppendLine(
            $$"""
              public partial class Program : {{dbContextClassName}}
              {
                  public Program()
                  {
                  }

                  public Program(DbContextOptions<{{dbContextClassName}}> options) : base(options)
                  {
                  }

                  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                  {
                      optionsBuilder
                          .EnableSensitiveDataLogging()
                          .LogTo(
                              output =>
                              {
                                  DumpExtension.Sink.SqlWrite(output + "\n");
                              }
                          );

                      base.OnConfiguring(optionsBuilder);
                      {{(efDbConnection.ScaffoldOptions?.OptimizeDbContext == true
                          ? $"optionsBuilder.UseModel({EntityFrameworkDatabaseScaffolder.DbContextCompiledModelName}.Instance);"
                          : string.Empty)}}

                      OnConfiguringPartial(optionsBuilder);
                  }

                  /// <summary>
                  /// Implement this partial method to configure 'OnConfiguring'.
                  /// </summary>
                  partial void OnConfiguringPartial(DbContextOptionsBuilder optionsBuilder);

                  /// <summary>
                  /// Calls DataContext.SaveChanges();
                  /// </summary>
                  public static int SaveChanges()
                  {
                      return DataContext.SaveChanges();
                  }

                  /// <summary>
                  /// Calls DataContext.SaveChangesAsync(CancellationToken cancellationToken);
                  /// </summary>
                  public static Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                  {
                      return DataContext.SaveChangesAsync(cancellationToken);
                  }
              """);

        // 2. Add the DbContext property
        code.AppendLine($$"""
                             /// <summary>
                             /// The DbContext instance used to access the database.
                             /// </summary>
                             public static {{dbContextClassName}} DataContext { get; } = new Program();

                         """);


        var dbContextCodeLines = dbContextCode.Split(Environment.NewLine).ToList();

        // 3. Add properties for all the generated DbSet's
        var programProperties = new List<string>();

        foreach (var line in dbContextCodeLines)
        {
            // We only need DbSet property lines
            if (!line.Contains("public virtual DbSet<")) continue;

            // Extracts 'DbSet<Book> Books' from 'public virtual DbSet<Book> Books { get; set; } = null!;'
            var typeAndName = line.SubstringBetween("virtual ", " {");
            var parts = typeAndName.Split(" ");

            var entityType = parts[0].SubstringBetween("<", ">");
            var propertyName = parts[1];

            programProperties.Add($"""
                                       /// <summary>
                                       /// The {propertyName} table (DbSet).
                                       /// </summary>
                                       public static Microsoft.EntityFrameworkCore.DbSet<{entityType}> {propertyName} => DataContext.{propertyName};
                                   """);
        }

        code.AppendJoin(Environment.NewLine, programProperties)
            .AppendLine()
            .AppendLine("}");

        var applicationCode = new SourceCode(code.ToString());
        applicationCode.AddUsing("Microsoft.EntityFrameworkCore");

        return new SourceCodeCollection([applicationCode]);
    }
}
