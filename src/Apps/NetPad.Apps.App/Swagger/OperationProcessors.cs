using System.Linq;
using System.Reflection;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace NetPad.Swagger;

internal class IncludeControllersInAssemblies : IOperationProcessor
{
    private readonly Assembly[] _assemblies;

    public IncludeControllersInAssemblies(params Assembly[] assemblies)
    {
        _assemblies = assemblies;
    }

    public bool Process(OperationProcessorContext context)
    {
        return _assemblies.Contains(context.ControllerType.Assembly);
    }
}

internal class ExcludeControllersInAssemblies : IOperationProcessor
{
    private readonly Assembly[] _assemblies;

    public ExcludeControllersInAssemblies(params Assembly[] assemblies)
    {
        _assemblies = assemblies;
    }

    public bool Process(OperationProcessorContext context)
    {
        return !_assemblies.Contains(context.ControllerType.Assembly);
    }
}
