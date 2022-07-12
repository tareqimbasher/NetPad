using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NetPad.Plugins;

public record PluginInitialization(IConfiguration Configuration, IHostEnvironment HostEnvironment);
