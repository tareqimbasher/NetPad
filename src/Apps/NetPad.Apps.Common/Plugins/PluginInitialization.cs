using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NetPad.Apps.Plugins;

public record PluginInitialization(IConfiguration Configuration, IHostEnvironment HostEnvironment);
