using System.Linq;
using System.Reflection;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace NetPad.Swagger;

internal class IncludeControllersInAssemblies(params Assembly[] assemblies) : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        return assemblies.Contains(context.ControllerType.Assembly);
    }
}

internal class ExcludeControllersInAssemblies(params Assembly[] assemblies) : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        return !assemblies.Contains(context.ControllerType.Assembly);
    }
}
