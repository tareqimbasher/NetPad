using System.Diagnostics;
using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class ProcessUtilTests
{
    [Fact]
    public void RemoveInheritedMSBuildSdkVariables_RemovesSdkPinningVariables_AndKeepsOthers()
    {
        var psi = new ProcessStartInfo("dotnet");
        psi.Environment["MSBuildExtensionsPath"] = "/sdk/10.0.100/";
        psi.Environment["MSBuildSDKsPath"] = "/sdk/10.0.100/Sdks";
        psi.Environment["MSBUILD_EXE_PATH"] = "/sdk/10.0.100/MSBuild.dll";
        psi.Environment["DOTNET_ROOT"] = "/dotnet";

        psi.RemoveInheritedMSBuildSdkVariables();

        Assert.False(psi.Environment.ContainsKey("MSBuildExtensionsPath"));
        Assert.False(psi.Environment.ContainsKey("MSBuildSDKsPath"));
        Assert.False(psi.Environment.ContainsKey("MSBUILD_EXE_PATH"));
        Assert.Equal("/dotnet", psi.Environment["DOTNET_ROOT"]);
    }
}
