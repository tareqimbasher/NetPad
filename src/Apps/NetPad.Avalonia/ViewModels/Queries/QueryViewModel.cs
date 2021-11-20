using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using NetPad.Exceptions;
using NetPad.Queries;
using NetPad.Runtimes;
using NetPad.Runtimes.Compilation;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueryViewModel : ViewModelBase
    {
        private readonly Query _query;
        private readonly IQueryRuntime _queryRuntime;
        private string _code;
        private string _results = string.Empty;
        public string Id { get; } = Guid.NewGuid().ToString();

        public QueryViewModel()
        {
        }

        public QueryViewModel(Query query, IQueryRuntime queryRuntime) : this()
        {
            _query = query;
            _queryRuntime = queryRuntime;
            Code = query.Code;

            this.WhenAnyValue(x => x.Code)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x => Query.UpdateCode(x));
        }

        public Query Query => _query;

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

        public async Task RunQueryAsync()
        {
            Results = string.Empty;

            await _queryRuntime.InitializeAsync(Query);

            try
            {
                await _queryRuntime.RunAsync(null, new TestQueryRuntimeOutputWriter(output =>
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

        public class TestQueryRuntimeOutputWriter : IQueryRuntimeOutputWriter
        {
            private readonly Action<object?> _action;

            public TestQueryRuntimeOutputWriter(Action<object?> action)
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
