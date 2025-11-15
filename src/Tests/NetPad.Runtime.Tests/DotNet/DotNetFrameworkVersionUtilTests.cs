using Microsoft.CodeAnalysis.CSharp;
using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class DotNetFrameworkVersionUtilTests
{
    [Theory]
    [InlineData("2.0.0", false)]
    [InlineData("4.0.0", false)]
    [InlineData("3.1.0", false)]
    [InlineData("5.0.0", false)]
    [InlineData("6.0.0", true)]
    [InlineData("8.0.0", true)]
    [InlineData("9.0.0", true)]
    public void Determines_Supported_SDK_Version_Correctly(string version, bool expectedSupported)
    {
        var semanticVersion = new SemanticVersion(version);
        var sdkVersion = new DotNetSdkVersion(semanticVersion);

        Assert.Equal(expectedSupported, DotNetFrameworkVersionUtil.IsSdkVersionSupported(semanticVersion));
        Assert.Equal(expectedSupported, DotNetFrameworkVersionUtil.IsSupported(sdkVersion));
    }

    [Theory]
    [InlineData("3.1.0", false)]
    [InlineData("5.0.0", true)]
    [InlineData("6.0.0", true)]
    [InlineData("8.0.0", true)]
    public void Determines_Supported_EfCoreTool_Version_Correctly(string version, bool expectedSupported)
    {
        var semanticVersion = new SemanticVersion(version);

        var supported = DotNetFrameworkVersionUtil.IsEfToolVersionSupported(semanticVersion);

        Assert.Equal(expectedSupported, supported);
    }

    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet5, "net5.0")]
    [InlineData(DotNetFrameworkVersion.DotNet6, "net6.0")]
    [InlineData(DotNetFrameworkVersion.DotNet7, "net7.0")]
    [InlineData(DotNetFrameworkVersion.DotNet8, "net8.0")]
    [InlineData(DotNetFrameworkVersion.DotNet9, "net9.0")]
    public void GetTargetFrameworkMoniker_Retruns_Correct_TFM(
        DotNetFrameworkVersion version,
        string expectedTfm)
    {
        var tfm = DotNetFrameworkVersionUtil.GetTargetFrameworkMoniker(version);
        Assert.Equal(expectedTfm, tfm);
    }

    [Theory]
    [InlineData("net5.0", DotNetFrameworkVersion.DotNet5)]
    [InlineData("net6.0", DotNetFrameworkVersion.DotNet6)]
    [InlineData("net7.0", DotNetFrameworkVersion.DotNet7)]
    [InlineData("net8.0", DotNetFrameworkVersion.DotNet8)]
    [InlineData("net9.0", DotNetFrameworkVersion.DotNet9)]
    public void GetFrameworkVersion_By_TFM_Retruns_Correct_FrameworkVersion(
        string tfm,
        DotNetFrameworkVersion expectedVersion)
    {
        var version = DotNetFrameworkVersionUtil.GetFrameworkVersion(tfm);
        Assert.Equal(expectedVersion, version);
    }

    [Theory]
    [InlineData("net100.0")]
    [InlineData("net6")]
    [InlineData("net6.1")]
    [InlineData("net6 0")]
    [InlineData("foobar")]
    public void GetFrameworkVersion_By_TFM_Throws_When_Invalid_TFM(string invalidTfm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DotNetFrameworkVersionUtil.GetFrameworkVersion(invalidTfm));
    }

    [Theory]
    [InlineData("net5.0", DotNetFrameworkVersion.DotNet5)]
    [InlineData("net6.0", DotNetFrameworkVersion.DotNet6)]
    [InlineData("net7.0", DotNetFrameworkVersion.DotNet7)]
    [InlineData("net8.0", DotNetFrameworkVersion.DotNet8)]
    [InlineData("net9.0", DotNetFrameworkVersion.DotNet9)]
    [InlineData("net6", null)]
    [InlineData("net100.0", null)]
    public void TryGetFrameworkVersion_By_TFM_Retruns_Correct_FrameworkVersion_Or_Null(
        string tfm,
        DotNetFrameworkVersion? expectedVersion)
    {
        DotNetFrameworkVersionUtil.TryGetFrameworkVersion(tfm, out var version);
        Assert.Equal(expectedVersion, version);
    }

    [Theory]
    [InlineData("Microsoft.NETCore.App", "6.0.0", DotNetFrameworkVersion.DotNet6)]
    [InlineData("Microsoft.AspNetCore.App", "6.0.0", DotNetFrameworkVersion.DotNet6)]
    [InlineData("Microsoft.NETCore.App", "8.0.0", DotNetFrameworkVersion.DotNet8)]
    public void GetFrameworkVersion_By_DotNetRuntimeVersion_Returns_Correct_Version(
        string frameworkName,
        string semanticVersion,
        DotNetFrameworkVersion expectedVersion)
    {
        var runtimeVersion = new DotNetRuntimeVersion(frameworkName, new SemanticVersion(semanticVersion));

        var frameworkVersion = DotNetFrameworkVersionUtil.GetFrameworkVersion(runtimeVersion);

        Assert.Equal(expectedVersion, frameworkVersion);
    }

    [Theory]
    [InlineData("6.0.0", DotNetFrameworkVersion.DotNet6)]
    [InlineData("7.0.0", DotNetFrameworkVersion.DotNet7)]
    [InlineData("8.0.0", DotNetFrameworkVersion.DotNet8)]
    [InlineData("9.0.0", DotNetFrameworkVersion.DotNet9)]
    [InlineData("10.0.0", DotNetFrameworkVersion.DotNet10)]
    public void GetFrameworkVersion_By_DotNetSdkVersion_Returns_Correct_Version(
        string semanticVersion,
        DotNetFrameworkVersion expectedVersion)
    {
        var sdkVersion = new DotNetSdkVersion(new SemanticVersion(semanticVersion));

        var frameworkVersion = DotNetFrameworkVersionUtil.GetFrameworkVersion(sdkVersion);

        Assert.Equal(expectedVersion, frameworkVersion);
    }

    [Theory]
    [InlineData(6, DotNetFrameworkVersion.DotNet6)]
    [InlineData(7, DotNetFrameworkVersion.DotNet7)]
    [InlineData(8, DotNetFrameworkVersion.DotNet8)]
    [InlineData(9, DotNetFrameworkVersion.DotNet9)]
    [InlineData(10, DotNetFrameworkVersion.DotNet10)]
    public void GetFrameworkVersion_By_MajorVersion_Returns_Correct_Version(
        int majorVersion,
        DotNetFrameworkVersion expectedVersion)
    {
        var frameworkVersion = DotNetFrameworkVersionUtil.GetFrameworkVersion(majorVersion);

        Assert.Equal(expectedVersion, frameworkVersion);
    }

    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet5, LanguageVersion.CSharp9)]
    [InlineData(DotNetFrameworkVersion.DotNet6, LanguageVersion.CSharp10)]
    [InlineData(DotNetFrameworkVersion.DotNet7, LanguageVersion.CSharp11)]
    [InlineData(DotNetFrameworkVersion.DotNet8, LanguageVersion.CSharp12)]
    [InlineData(DotNetFrameworkVersion.DotNet9, LanguageVersion.CSharp13)]
    [InlineData(DotNetFrameworkVersion.DotNet10, LanguageVersion.Preview)]
    public void GetLatestSupportedCSharpLanguageVersion_Returns_Correct_LangVersion(
        DotNetFrameworkVersion frameworkVersion,
        LanguageVersion expectedLangVersion)
    {
        var langVersion = DotNetFrameworkVersionUtil.GetLatestSupportedCSharpLanguageVersion(frameworkVersion);
        Assert.Equal(expectedLangVersion, langVersion);
    }
}
