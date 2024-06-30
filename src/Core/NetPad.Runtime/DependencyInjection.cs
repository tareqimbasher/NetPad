using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetPad.Application;
using NetPad.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad;

public class DataConnectionFeatureBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}

internal static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<AppIdentifier>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<IDotNetInfo, DotNetInfo>();
        services.TryAddTransient<ICodeCompiler, CSharpCodeCompiler>();
        services.TryAddTransient<ICodeAnalysisService, CodeAnalysisService>();
        services.TryAddSingleton<IAppStatusMessagePublisher, AppStatusMessagePublisher>();

        return services;
    }

    public static DataConnectionFeatureBuilder AddDataConnectionFeature<
        TIDataConnectionRepository,
        TIDataConnectionResourcesRepository,
        TIDataConnectionResourcesCache
    >(this IServiceCollection services)
        where TIDataConnectionRepository : class, IDataConnectionRepository
        where TIDataConnectionResourcesRepository : class, IDataConnectionResourcesRepository
        where TIDataConnectionResourcesCache : class, IDataConnectionResourcesCache
    {
        services.AddDataProtection(options => options.ApplicationDiscriminator = AppIdentifier.AppId);

        services.AddTransient<IDataConnectionRepository, TIDataConnectionRepository>();
        services.AddTransient<IDataConnectionResourcesRepository, TIDataConnectionResourcesRepository>();
        services.AddSingleton<IDataConnectionResourcesCache, TIDataConnectionResourcesCache>();

        services.AddTransient<IDataConnectionPasswordProtector>(s =>
            new DataProtector(s.GetRequiredService<IDataProtectionProvider>(), "DataConnectionPasswords"));

        services.AddSingleton(sp =>
            new Lazy<IDataConnectionResourcesCache>(sp.GetRequiredService<IDataConnectionResourcesCache>()));

        services.AddTransient<
            IDataConnectionSchemaChangeDetectionStrategyFactory,
            DataConnectionSchemaChangeDetectionStrategyFactory>();

        return new DataConnectionFeatureBuilder(services);
    }
}
