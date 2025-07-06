using NetPad.DotNet.References;
using NetPad.Exceptions;
using NetPad.Tests.Helpers;
using Xunit;

namespace NetPad.Runtime.Tests.Exceptions;

public class ExceptionsTests
{
    [Fact]
    public void EnvironmentNotFoundException_Basic()
    {
        _ = new EnvironmentNotFoundException(new Guid());
    }

    [Fact]
    public void InvalidReferenceException_Basic()
    {
        _ = new InvalidReferenceException(new AssemblyFileReference(string.Empty), "message");
    }

    [Fact]
    public void InvalidScriptFormatException_Basic()
    {
        _ = new InvalidScriptFormatException("script name", "message");
        _ = new InvalidScriptFormatException(ScriptTestHelper.CreateScript(), "message");
    }

    [Fact]
    public void ScriptNotFoundException_Basic()
    {
        _ = new ScriptNotFoundException(new Guid());
        _ = new ScriptNotFoundException("path");
    }

    [Fact]
    public void ScriptRuntimeException_Basic()
    {
        _ = new ScriptNotFoundException("message");
    }
}
