using System;
using NetPad.Scripts;

namespace NetPad.Tests.Helpers;

public static class ScriptTestHelper
{
    public static Script CreateScript(Guid? id = null, string? name = null)
    {
        id ??= Guid.NewGuid();
        name ??= $"Script {id}";
        return new Script(id.Value, name);
    }
}
