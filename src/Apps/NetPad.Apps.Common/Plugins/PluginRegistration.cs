using System.Reflection;

namespace NetPad.Apps.Plugins;

public record PluginRegistration(Assembly Assembly, IPlugin Plugin);
