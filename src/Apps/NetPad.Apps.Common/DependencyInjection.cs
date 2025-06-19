using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetPad.Application;
using NetPad.Apps.Configuration;
using NetPad.Apps.Data;
using NetPad.Apps.Scripts;
using NetPad.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Packages;
using NetPad.Packages.NuGet;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Apps;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ISession, Session>();
        services.TryAddSingleton<AppIdentifier>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<IDotNetInfo, DotNetInfo>();
        services.TryAddTransient<ICodeCompiler, CSharpCodeCompiler>();
        services.TryAddTransient<ICodeAnalysisService, CodeAnalysisService>();
        services.TryAddSingleton<IAppStatusMessagePublisher, AppStatusMessagePublisher>();
        services.AddTransient<ISettingsRepository, FileSystemSettingsRepository>();
        services.AddSingleton<Settings>(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);
        services.AddTransient<IScriptRepository, FileSystemScriptRepository>();
        services.AddSingleton<IScriptNameGenerator, DefaultScriptNameGenerator>();
        services.AddTransient<IPackageProvider, NuGetPackageProvider>();
        services.AddDataProtection(options => options.ApplicationDiscriminator = AppIdentifier.AppId);

        return services;
    }

    public static DataConnectionFeatureBuilder AddDataConnectionFeature(this IServiceCollection services)
    {
        services.AddTransient<IDataConnectionRepository, FileSystemDataConnectionRepository>();
        services.AddTransient<IDataConnectionResourcesRepository, FileSystemDataConnectionResourcesRepository>();
        services.AddSingleton<IDataConnectionResourcesCache, FileSystemDataConnectionResourcesCache>();
        services.AddSingleton<Lazy<IDataConnectionResourcesCache>>(sp =>
            new Lazy<IDataConnectionResourcesCache>(sp.GetRequiredService<IDataConnectionResourcesCache>()));

        services.AddTransient<IDataConnectionPasswordProtector>(s =>
            new DataProtector(s.GetRequiredService<IDataProtectionProvider>(), "DataConnectionPasswords"));

        services.AddTransient<
            IDataConnectionSchemaChangeDetectionStrategyFactory,
            DataConnectionSchemaChangeDetectionStrategyFactory>();

        return new DataConnectionFeatureBuilder(services);
    }
}

public class DataConnectionFeatureBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}
