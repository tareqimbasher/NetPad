using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes.Compilation
{
    public class CodeCompilerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CodeCompilerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Can_Compile_EmptyProgram()
        {
            var code = Wrap("");

            var assemblyBytes = Compile(new CompilationInput(code));
            
            Assert.NotEmpty(assemblyBytes);
        }
        
        [Fact]
        public void Can_Compile_SimpleProgram()
        {
            var code = Wrap("Console.WriteLine(\"Hello World\");");

            var assemblyBytes = Compile(new CompilationInput(code));
            
            Assert.NotEmpty(assemblyBytes);
        }
        
        [Theory]
        [InlineData("Console.Write(\"Hello World\";")]
        [InlineData("Console.Write(\"Hello World\")")]
        [InlineData("Console.(\"Hello World\")")]
        [InlineData("Console.WriteE(\"Hello World\")")]
        [InlineData("Console.write(\"Hello World\");")]
        [InlineData("foobar")]
        public void Fails_On_Syntax_Error(string code)
        {
            var compiler = new CodeCompiler();

            code = Wrap(code);

            Assert.Throws<CodeCompilationException>(() => compiler.Compile(new CompilationInput(code)));
        }

        [Theory]
        [InlineData("using System.Threading.Tasks", "var task = Task.CompletedTask;")]
        [InlineData("using System.Collections.Generic", "var list = new List<string>();")]
        public void Can_Compile_Program_With_DotNet_Usings(string @namespace, string code)
        {
            code = Wrap(code);
            code = $"{@namespace};\n" + code;

            var assemblyBytes = Compile(new CompilationInput(code));
            
            Assert.NotEmpty(assemblyBytes);
        }
        
        [Fact]
        public void Can_Compile_CSharp8_Features()
        {
            var code = Wrap("using var stream = new MemoryStream();");

            var assemblyBytes = Compile(new CompilationInput(code));
            
            Assert.NotEmpty(assemblyBytes);
        }
        
        [Fact]
        public void Can_Compile_CSharp9_Features()
        {
            var code = Wrap("DateTime datetime = new();");

            var assemblyBytes = Compile(new CompilationInput(code));
            
            Assert.NotEmpty(assemblyBytes);
        }
        
        [Fact]
        public void Can_Not_Compile_CSharp10_Features()
        {
            var compiler = new CodeCompiler();

            var code = Wrap("var point = (1, 2); int x = 0; (x, int y) = point;");

            Assert.Throws<CodeCompilationException>(() => compiler.Compile(new CompilationInput(code)));
        }

        private byte[] Compile(CompilationInput input)
        {
            try
            {
                var compiler = new CodeCompiler();
                return compiler.Compile(input);
            }
            catch (CodeCompilationException ex)
            {
                _testOutputHelper.WriteLine(ex.ErrorsAsString());
                throw;
            }
        }

        
        private string Wrap(string code)
        {
            return $@"
using System;
using System.IO;
public class Program
{{
    public void Main()
    {{
        {code}
    }}
}}
";
        }
    }
}