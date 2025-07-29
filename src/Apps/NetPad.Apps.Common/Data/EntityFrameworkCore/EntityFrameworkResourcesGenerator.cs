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

/// <summary>
/// Generates the resources (assembly, code...etc.) needed to use a <see cref="EntityFrameworkDatabaseConnection"/>.
/// </summary>
internal class EntityFrameworkResourcesGenerator(
    IDataConnectionPasswordProtector dataConnectionPasswordProtector,
    IDotNetInfo dotNetInfo,
    Settings settings,
    ILoggerFactory loggerFactory)
    : IDataConnectionResourcesGenerator
{
    public async Task<DataConnectionResources> GenerateResourcesAsync(
        DataConnection dataConnection,
        DotNetFrameworkVersion targetFrameworkVersion)
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

        var applicationCode = GenerateApplicationCode(
            efDbConnection, result.Model.DbContextFile.ClassName,
            result.Model.DbContextFile.Code.ToCodeString());

        var sourceCode = new DataConnectionSourceCode
        {
            ApplicationCode = applicationCode
        };

        var requiredReferences =
            EntityFrameworkPackageUtils.GetRequiredReferences(efDbConnection, targetFrameworkVersion);

        return new DataConnectionResources(efDbConnection, DateTime.UtcNow)
        {
            SourceCode = sourceCode,
            Assembly = result.Assembly,
            RequiredReferences = requiredReferences,
            DatabaseStructure = result.DatabaseStructure
        };
    }

    /// <summary>
    /// Generates code that will be needed to access the database using the resources generated for the data connection.
    /// </summary>
    private static SourceCodeCollection GenerateApplicationCode(
        EntityFrameworkDatabaseConnection efDbConnection,
        string dbContextClassName,
        string dbContextCode)
    {
        // We want to generate code that can be used to augment the "partial Program class" in scripts have implicitly.
        // The goal is to accomplish the following items, mainly for convenience while writing scripts:
        // 1. Make the Program class inherit the "DbContext" that was generated during scaffolding.
        //    Why? - This allows users to override methods on the base DbContext, ex: the OnConfiguring(DbContextOptionsBuilder optionsBuilder) method.
        //
        // 2. Add a property to the Program that points to an instance of the generated DbContext.
        //    Why? - This allows users to access the DbContext instance via a property on the top-level Program.
        //
        // 3. Add properties for all the generated DbSet's to the Program
        //    Why? - This makes it easy for users to just type the name of the table/DbSet (ex: "Authors") in their query
        //           instead of having to do something like "DbContext.Authors"

        var code = new StringBuilder();

        // 1. Make the Program class inherit the generated DbContext
        // The "partial Program class" here will augment the Program class implicitly available to scripts.
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

                  /// <summary>
                  /// Overriden by NetPad. Implement "partial void OnConfiguringPartial(DbContextOptionsBuilder optionsBuilder)"
                  /// to add code to "OnConfiguring".
                  /// </summary>
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



        // 3. Add properties for all the generated DbSet's
        var programProperties = new List<string>();
        var dbContextCodeLines = dbContextCode.Split(Environment.NewLine).ToList();

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
