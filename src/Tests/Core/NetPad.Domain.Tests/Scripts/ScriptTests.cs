using System;
using System.Text.RegularExpressions;
using NetPad.Scripts;
using Xunit;

namespace NetPad.Domain.Tests.Scripts
{
    public class ScriptTests
    {
        [Fact]
        public void Defines_Correct_Standard_Extension_Names()
        {
            Assert.Equal(".netpad", Script.STANARD_EXTENSION);
            Assert.Equal("netpad", Script.STANARD_EXTENSION_WO_DOT);
        }

        [Fact]
        public void Ctor1_Id_Param_Gets_Set()
        {
            var id = Guid.NewGuid();

            var script = new Script(id, "Test");

            Assert.Equal(id, script.Id);
        }

        [Fact]
        public void Ctor1_Name_Param_Gets_Set()
        {
            var name = "Test Name";

            var script = new Script(Guid.NewGuid(), name);

            Assert.Equal(name, script.Name);
        }

        [Fact]
        public void Ctor2_Name_Param_Gets_Set()
        {
            var name = "Test Name";

            var script = new Script(name);

            Assert.Equal(name, script.Name);
        }

        [Fact]
        public void Config_Is_Not_Null_When_Script_Instantiated()
        {
            Assert.NotNull(new Script("Test").Config);
        }

        [Fact]
        public void Code_Is_Empty_String_When_Script_Instantiated()
        {
            Assert.Equal("", new Script("Test").Code);
        }

        [Fact]
        public void Path_Is_Null_When_Script_Instantiated()
        {
            Assert.Null(new Script("Test").Path);
        }

        [Fact]
        public void DirectoryPath_Is_Null_When_Script_Instantiated()
        {
            Assert.Null(new Script("Test").DirectoryPath);
        }

        [Fact]
        public void OnPropertyChanged_Has_No_Handlers_When_Script_Instantiated()
        {
            Assert.Empty(new Script("Test").OnPropertyChanged);
        }

        [Fact]
        public void Kind_Is_Statements_When_Script_Instantiated()
        {
            Assert.Equal(ScriptKind.Statements, new Script("Test").Config.Kind);
        }

        [Fact]
        public void No_Namespaces_When_Script_Instantiated()
        {
            Assert.Empty(new Script("Test").Config.Namespaces);
        }

        [Fact]
        public void IsNew_Is_True_When_Path_Is_Null()
        {
            Assert.True(new Script("Test").IsNew);
        }

        [Fact]
        public void IsNew_Is_False_When_Path_Is_Not_Null()
        {
            var script = new Script("Test");
            script.SetPath("Some path");

            Assert.False(script.IsNew);
        }

        [Fact]
        public void UpdateCode_Sets_Code()
        {
            var code = "Some code";
            var script = new Script("Test");

            script.UpdateCode(code);

            Assert.Equal(code, script.Code);
        }

        [Fact]
        public void SetPath_Sets_Path()
        {
            var path = $"/some/path/test.{Script.STANARD_EXTENSION_WO_DOT}";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.Equal(path, script.Path);
        }

        [Fact]
        public void SetPath_Prepends_Forward_Slash_If_Not_Added_Already()
        {
            var path = $"some/path/test.{Script.STANARD_EXTENSION_WO_DOT}";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.Equal("/" + path, script.Path);
        }

        [Fact]
        public void SetPath_Does_Not_Prepend_Forward_Slash_If_Added_Already()
        {
            var path = $"some/path/test.{Script.STANARD_EXTENSION_WO_DOT}";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.NotEqual('/', script.Path![1]);
        }

        [Fact]
        public void SetPath_Adds_File_Extension_To_Path_If_Not_Added_Already()
        {
            var path = "/some/path/test";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.Equal(path + Script.STANARD_EXTENSION, script.Path);
        }

        [Fact]
        public void SetPath_Does_Not_Add_File_Extension_To_Path_If_Added_Already()
        {
            var path = $"/some/path/test.{Script.STANARD_EXTENSION_WO_DOT}";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.Single(Regex.Matches(script.Path!, Script.STANARD_EXTENSION));
        }

        [Fact]
        public void SetPath_Sets_Name()
        {
            var path = $"/some/path/test.{Script.STANARD_EXTENSION_WO_DOT}";
            var script = new Script("Test");

            script.SetPath(path);

            Assert.Equal("test", script.Name);
        }

        // [Fact]
        // public void Load_Loads_Id()
        // {
        //
        // }
    }
}
