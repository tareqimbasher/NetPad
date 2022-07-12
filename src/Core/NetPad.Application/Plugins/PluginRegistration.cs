using System.Reflection;

namespace NetPad.Plugins;

public record PluginRegistration(Assembly Assembly, IPlugin Plugin);
