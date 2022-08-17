using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.Scaffolding;

namespace NetPad.Data;

public class DatabaseConnectionSourceCodeCache : IDataConnectionSourceCodeCache
{
    private readonly Settings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<Guid, SourceCodeCollection> _sourceCode;

    public DatabaseConnectionSourceCodeCache(Settings settings, ILoggerFactory loggerFactory)
    {
        _settings = settings;
        _loggerFactory = loggerFactory;
        _sourceCode = new Dictionary<Guid, SourceCodeCollection>();
    }

    public async Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection)
    {
        if (_sourceCode.TryGetValue(dataConnection.Id, out var sourceCode))
        {
            return sourceCode;
        }

        if (dataConnection is not EntityFrameworkDatabaseConnection efDbConnection)
        {
            return new SourceCodeCollection();
        }

        var scaffolder = new EntityFrameworkDatabaseScaffolder(efDbConnection, _settings, _loggerFactory.CreateLogger<EntityFrameworkDatabaseScaffolder>());
        var success = await scaffolder.ScaffoldAsync();

        if (!success)
        {
            throw new Exception("Database connection could not be scaffolded.");
        }

        var model = await scaffolder.GetScaffoldedModelAsync();
        sourceCode = new SourceCodeCollection(model.SourceFiles);

        _sourceCode.Add(dataConnection.Id, sourceCode);

        return sourceCode;
    }
}
