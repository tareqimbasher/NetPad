using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using NetPad.Queries;
using NetPad.Sessions;
using ReactiveUI;

namespace NetPad.ViewModels.Queries
{
    public class QueriesViewModel : ViewModelBase
    {
        private readonly IQueryManager _queryManager;

        public QueriesViewModel()
        {
        }

        public QueriesViewModel(IQueryManager queryManager, Session session)
        {
            _queryManager = queryManager;
            
            _queries = session.OpenQueries
                .ToObservableChangeSet().ToCollection()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(queries => queries.Select(query => new QueryViewModel()
                {
                    Query = query
                }))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Queries, scheduler: RxApp.MainThreadScheduler);
            
            _queries.ThrownExceptions.Subscribe(ex =>
            {
                Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            });
        }

        private readonly ObservableAsPropertyHelper<IEnumerable<QueryViewModel>> _queries;
        public List<QueryViewModel> Queries => _queries.Value.ToList();
        
        public async Task CreateNewQueryAsync()
        {
            await _queryManager.CreateNewQueryAsync();
        }
    }
}