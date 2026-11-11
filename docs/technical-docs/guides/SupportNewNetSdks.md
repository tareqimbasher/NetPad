# Supporting new .NET SDK versions

When a new .NET SDK is released, we need to add support for it so that NetPad can recognize it, and allow users to write
scripts that target this new .NET SDK version. This guide explains the code changes needed to add support for a new .NET
SDK to NetPad.

### Steps

1. Add the new version to the `DotNetFrameworkVersion.cs` enum.
2. Update `DotNetFrameworkVersionUtil.cs`:
    - Raise `MaxSupportedDotNetVersion`.
    - Add the new version to `_majorToFrameworkVersion`, `_frameworkVersionToTfm` and `_frameworkVersionToLangVersion`.
    - For the language version, update the `Microsoft.CodeAnalysis.CSharp` package in `NetPad.Runtime.csproj` to a
      release that defines the new `LanguageVersion`. If NuGet has no such release yet, use `LanguageVersion.Preview`
      and replace it once one is published. If the previous .NET version is still on `Preview`, give it its real
      `LanguageVersion` in the same change.
3. Add the new version to each provider's table in `EntityFrameworkPackageUtils.cs`.
    - Use the provider's newest stable release for that .NET version. If it has none yet, use its newest stable release
      for an older .NET version.
    - Only the provider is listed. When a data connection is scaffolded, NetPad reads the EF Core version the provider
      resolved to and adds `Microsoft.EntityFrameworkCore.Design` and `Microsoft.EntityFrameworkCore.Tools` at that
      same version.
4. Regenerate the TypeScript API client with `just dotnet-ts-gen`. The build fails in CI if it is out of date.
5. Add the SDK to the `dotnet-version` list in `.github/workflows/build.yml`. Some tests compile and run code against
   every .NET version a script can target.
6. Add unit tests:
    - `CodeAnalysisServiceTests.cs`: Add a test case to `Compiler_Uses_Correct_CSharp_LanguageVersion()`.
    - `CSharpCodeCompilerTests`: Add a new unit test `Can_Compile_CSharpX_Features` (ex: `X` = 12 for C# 12) to test
      compiling new C# language version. Make sure to use code that is only valid in the new C# language version. Also
      add the new version to `Runs_Framework_Source_Generators`.
    - Add the new version to the lists in `DotNetFrameworkVersionUtilTests.cs`, `DotNetInfoExtensionsTests.cs` and
      `EntityFrameworkPackageUtilsTests.cs`.

### Preview and release candidate SDKs

Support can be added before the SDK's final release. A few things differ until then:

- NetPad lists an installed preview SDK as a target, but new scripts keep defaulting to the newest stable SDK.
- Do not use prerelease provider packages in `EntityFrameworkPackageUtils.cs`. Use the fallback from step 3 and update
  the table when the final packages are published.
- In `build.yml`, an entry like `11.0.x` only installs a final release. For a preview, list its exact version
  (ex: `11.0.100-rc.2.26504.105`) and change it to `11.0.x` after the final release.

### Test it

After the code changes are in place, test it:

1. Run NetPad and start a new script.
2. Select the new .NET SDK version.
3. Write some code and run it. Dumping `Environment.Version` confirms which runtime the script ran on.
4. Check that code completion and error highlighting work in that script.
5. Create a data connection, use it in a script that targets the new version, and run a LINQ query.
6. Run `just dotnet-test-all` (needs Docker). It scaffolds data connections on the newest installed SDK.

> If you're having issues, and need help, please reach out via Discord.
