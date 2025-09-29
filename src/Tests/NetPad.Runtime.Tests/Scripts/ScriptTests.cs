using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using NetPad.DotNet;
using NetPad.Scripts;
using NetPad.Tests.Helpers;
using Xunit;

namespace NetPad.Runtime.Tests.Scripts;

public class ScriptTests
{
    [Fact]
    public void Defines_Correct_Standard_Extension_Names()
    {
        Assert.Equal(".netpad", Script.STANDARD_EXTENSION);
        Assert.Equal("netpad", Script.STANDARD_EXTENSION_WO_DOT);
    }

    [Fact]
    public void Ctor_Id_Param_Gets_Set()
    {
        var id = Guid.NewGuid();

        var script = ScriptTestHelper.CreateScript(id);

        Assert.Equal(id, script.Id);
    }

    [Fact]
    public void Ctor_Name_Param_Gets_Set()
    {
        var name = "Test Name";

        var script = ScriptTestHelper.CreateScript(Guid.NewGuid(), name);

        Assert.Equal(name, script.Name);
    }

    [Fact]
    public void Config_Is_Not_Null_When_Script_Instantiated()
    {
        Assert.NotNull(ScriptTestHelper.CreateScript().Config);
    }

    [Fact]
    public void Code_Is_Empty_String_When_Script_Instantiated()
    {
        Assert.Equal("", ScriptTestHelper.CreateScript().Code);
    }

    [Fact]
    public void Path_Is_Null_When_Script_Instantiated()
    {
        Assert.Null(ScriptTestHelper.CreateScript().Path);
    }

    [Fact]
    public void DirectoryPath_Is_Null_When_Script_Instantiated()
    {
        Assert.Null(ScriptTestHelper.CreateScript().DirectoryPath);
    }

    [Fact]
    public void OnPropertyChanged_Has_No_Handlers_When_Script_Instantiated()
    {
        Assert.Empty(ScriptTestHelper.CreateScript().OnPropertyChanged);
    }

    [Fact]
    public void Kind_Is_Program_When_Script_Instantiated()
    {
        Assert.Equal(ScriptKind.Program, ScriptTestHelper.CreateScript().Config.Kind);
    }

    [Fact]
    public void No_Namespaces_When_Script_Instantiated()
    {
        Assert.Empty(ScriptTestHelper.CreateScript().Config.Namespaces);
    }

    [Fact]
    public void IsNew_Is_True_When_Path_Is_Null()
    {
        Assert.True(ScriptTestHelper.CreateScript().IsNew);
    }

    [Fact]
    public void IsNew_Is_False_When_Path_Is_Not_Null()
    {
        var script = ScriptTestHelper.CreateScript();
        script.SetPath("Some path");

        Assert.False(script.IsNew);
    }

    [Fact]
    public void UpdateCode_Sets_Code()
    {
        var code = "Some code";
        var script = ScriptTestHelper.CreateScript();

        script.UpdateCode(code);

        Assert.Equal(code, script.Code);
    }

    [Fact]
    public void SetPath_Sets_Path()
    {
        var path = $"/some/path/test.{Script.STANDARD_EXTENSION_WO_DOT}";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.Equal(path, script.Path);
    }

    [Fact]
    public void SetPath_Normalizes_Windows_Path_Separators()
    {
        var path = $"C:\\some\\path\\test.{Script.STANDARD_EXTENSION_WO_DOT}";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.Equal(path.Replace("\\", "/"), script.Path);
    }

    [Fact]
    public void SetPath_Does_Not_Prepend_Forward_Slash_If_Added_Already()
    {
        var path = $"some/path/test.{Script.STANDARD_EXTENSION_WO_DOT}";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.NotEqual('/', script.Path![1]);
    }

    [Fact]
    public void SetPath_Adds_File_Extension_To_Path_If_Not_Added_Already()
    {
        var path = "/some/path/test";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.Equal(path + Script.STANDARD_EXTENSION, script.Path);
    }

    [Fact]
    public void SetPath_Does_Not_Add_File_Extension_To_Path_If_Added_Already()
    {
        var path = $"/some/path/test.{Script.STANDARD_EXTENSION_WO_DOT}";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.Single(Regex.Matches(script.Path!, Script.STANDARD_EXTENSION));
    }

    [Fact]
    public void SetPath_Sets_Name()
    {
        var path = $"/some/path/test.{Script.STANDARD_EXTENSION_WO_DOT}";
        var script = ScriptTestHelper.CreateScript();

        script.SetPath(path);

        Assert.Equal("test", script.Name);
    }

    [Fact]
    public void UpdateConfig()
    {
        var script = ScriptTestHelper.CreateScript();
        Assert.NotEqual(ScriptKind.SQL, script.Config.Kind);
        Assert.NotEqual(DotNetFrameworkVersion.DotNet5, script.Config.TargetFrameworkVersion);
        Assert.NotEqual(OptimizationLevel.Release, script.Config.OptimizationLevel);
        Assert.False(script.Config.UseAspNet);

        var newConfig = new ScriptConfig(ScriptKind.SQL, DotNetFrameworkVersion.DotNet5)
            .SetOptimizationLevel(OptimizationLevel.Release)
            .SetUseAspNet(true);

        script.UpdateConfig(newConfig);

        Assert.Equal(ScriptKind.SQL, script.Config.Kind);
        Assert.Equal(DotNetFrameworkVersion.DotNet5, script.Config.TargetFrameworkVersion);
        Assert.Equal(OptimizationLevel.Release, script.Config.OptimizationLevel);
        Assert.True(script.Config.UseAspNet);
    }
}
