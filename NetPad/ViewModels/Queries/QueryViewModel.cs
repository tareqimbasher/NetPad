using System;
using System.Reactive.Linq;
using DynamicData.Binding;
using NetPad.Queries;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueryViewModel : ViewModelBase
    {
        private readonly Query _query;
        private string _code;

        public QueryViewModel()
        {
        }
        
        public QueryViewModel(Query query)
        {
            _query = query;
            Code = query.Code;

            this.WhenAnyValue(x => x.Code)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x => Query.UpdateCodeAsync(x));
        }

        public Query Query => _query;

        public string Code
        {
            get => _code;
            set => this.RaiseAndSetIfChanged(ref _code, value);
        }
    }
}