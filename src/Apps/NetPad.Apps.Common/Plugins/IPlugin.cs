using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Apps.Plugins;

public interface IPlugin
{
    /// <summary>
    /// A unique plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// A display-friendly name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Add or edit service registrations.
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configure application.
    /// </summary>
    void Configure(IApplicationBuilder app, IHostEnvironment env);

    /// <summary>
    /// Invokes any cleanup the plugins needs to make.
    /// </summary>
    Task CleaupAsync();
}
