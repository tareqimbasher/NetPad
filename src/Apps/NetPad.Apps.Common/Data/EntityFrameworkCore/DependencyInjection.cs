using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore;

public static class DependencyInjection
{
    public static DataConnectionFeatureBuilder AddEntityFrameworkCoreDataConnectionDriver(this DataConnectionFeatureBuilder builder)
    {
        var services = builder.Services;

        services.AddTransient<IDatabaseConnectionMetadataProviderFactory, EntityFrameworkConnectionMetadataProviderFactory>();
        services.AddTransient<EntityFrameworkDatabaseConnectionMetadataProvider>();
        services.AddTransient<IDataConnectionResourcesGeneratorFactory, EntityFrameworkConnectionResourcesGeneratorFactory>();
        services.AddTransient<EntityFrameworkResourcesGenerator>();

        services.AddTransient<IDataConnectionSchemaChangeDetectionStrategy, MsSqlServerDatabaseSchemaChangeDetectionStrategy>();
        services.AddTransient<IDataConnectionSchemaChangeDetectionStrategy, PostgreSqlDatabaseSchemaChangeDetectionStrategy>();
        services.AddTransient<IDataConnectionSchemaChangeDetectionStrategy, SQLiteDatabaseSchemaChangeDetectionStrategy>();

        return builder;
    }
}
