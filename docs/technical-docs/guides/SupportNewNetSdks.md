# Supporting new .NET SDK versions

When a new .NET SDK is released, we need to add support for it so that NetPad can recognize it, and allow users to write
scripts that target this new .NET SDK version. This guide explains the code changes needed to add support for a new .NET
SDK to NetPad.

### Steps

1. Add new version to `DotNetFrameworkVersion.cs` enum.
2. Update `DotNetFrameworkVersionUtil.cs` and add proper entries for new .NET SDK version.
    - If needed, update `Microsoft.CodeAnalysis.CSharp` package in `NetPad.Domain.csproj` to get the latest
      `LanguageVersion` when updating the `_frameworkVersionToLangVersion` field.
3. Update `EntityFrameworkPackageUtils.cs` method for new .NET version.
4. Add unit tests:
    - `CodeAnalysisServiceTests.cs`: Add a test case to `Compiler_Uses_Correct_CSharp_LanguageVersion()`.
    - `CSharpCodeCompilerTests`: Add a new unit test `Can_Compile_CSharpX_Features` (ex: `X` = 12 for C# 12) to test
      compiling new C# language version. Make sure to use code that is only valid in the new C# language version.

### Test it

After the code changes are in place, test it:

1. Run NetPad and start a new script.
2. Select the new .NET SDK version.
3. Write some code and run it.

> If you're having issues, and need help, please reach out via Discord.