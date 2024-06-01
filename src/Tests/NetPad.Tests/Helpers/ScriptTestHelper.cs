using NetPad.Common;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Tests.Helpers;

public static class ScriptTestHelper
{
    public static Script CreateScript(Guid? id = null, string? name = null, DotNetFrameworkVersion? frameworkVersion = null)
    {
        id ??= Guid.NewGuid();
        name ??= $"Script {id}";
        frameworkVersion ??= GlobalConsts.AppDotNetFrameworkVersion;
        return new Script(id.Value, name, new ScriptConfig(ScriptKind.Program, frameworkVersion.Value));
    }
}
