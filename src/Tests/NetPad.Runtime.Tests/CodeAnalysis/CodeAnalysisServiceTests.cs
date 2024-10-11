using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NetPad.CodeAnalysis;
using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.CodeAnalysis;

public class CodeAnalysisServiceTests
{
    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet8, LanguageVersion.CSharp12)]
    public void Compiler_Uses_Correct_CSharp_LanguageVersion(DotNetFrameworkVersion targetFrameworkVersion, LanguageVersion? expectedLangVersion)
    {
        var codeAnalysisService = new CodeAnalysisService();

        if (expectedLangVersion == null)
        {
            Assert.ThrowsAny<Exception>(() => codeAnalysisService.GetParseOptions(targetFrameworkVersion, OptimizationLevel.Debug));
        }
        else
        {
            CSharpParseOptions parseOptions = codeAnalysisService.GetParseOptions(targetFrameworkVersion, OptimizationLevel.Debug);
            Assert.Equal(expectedLangVersion, parseOptions.LanguageVersion);
        }
    }
}
