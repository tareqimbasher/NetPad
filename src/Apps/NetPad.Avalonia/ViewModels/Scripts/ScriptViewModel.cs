using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Runtimes.Compilation;
using ReactiveUI;

namespace NetPad.ViewModels.Scripts
{
    public class ScriptViewModel : ViewModelBase
    {
        private readonly Script _script;
        private readonly IScriptRuntime _scriptRuntime;
        private string _code;
        private string _results = string.Empty;
        public string Id { get; } = Guid.NewGuid().ToString();

        public ScriptViewModel()
        {
        }

        public ScriptViewModel(Script script, IScriptRuntime scriptRuntime) : this()
        {
            _script = script;
            _scriptRuntime = scriptRuntime;
            Code = script.Code;

            this.WhenAnyValue(x => x.Code)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x => Script.UpdateCode(x));
        }

        public Script Script => _script;

        public string Code
        {
            get => _code;
            set => this.RaiseAndSetIfChanged(ref _code, value);
        }

        public string Results
        {
            get => _results;
            set => this.RaiseAndSetIfChanged(ref _results, value);
        }

        public async Task RunScriptAsync()
        {
            Results = string.Empty;

            await _scriptRuntime.InitializeAsync(Script);

            try
            {
                await _scriptRuntime.RunAsync(null, new TestScriptRuntimeOutputWriter(output =>
                {
                    Results += output;
                }));
            }
            catch (CodeCompilationException ex)
            {
                Results += ex.ErrorsAsString() + "\n";
            }
            catch (Exception ex)
            {
                Results += ex + "\n";
            }
        }

        public class TestScriptRuntimeOutputWriter : IScriptRuntimeOutputWriter
        {
            private readonly Action<object?> _action;

            public TestScriptRuntimeOutputWriter(Action<object?> action)
            {
                _action = action;
            }

            public Task WriteAsync(object? output)
            {
                _action(output);
                return Task.CompletedTask;
            }
        }
    }
}
