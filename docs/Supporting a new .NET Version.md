# Adding Support for a new .NET Version

To add support for a new version of .NET to NetPad use the following steps:

1. Update `DotNetFrameworkVersionUtil` and add proper entries for new .NET version.
2. Update `GlobalConsts.EntityFrameworkLibVersion` method for new .NET version.
3. If needed, update `Microsoft.CodeAnalysis.CSharp` package in `NetPad.Domain.csproj`.
4. `CSharpCodeCompilerTests`:
   - Add a test case to `Compiler_Uses_Correct_CSharp_LanguageVersion()`.
   - Add a new unit test `Can_Compile_CSharpX_Features` (ex: `X` = 12 for C# 12) to test compiling new C# language version.

Done.