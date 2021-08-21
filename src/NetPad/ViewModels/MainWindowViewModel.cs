using NetPad.Queries;
using NetPad.Sessions;
using NetPad.ViewModels.Queries;

namespace NetPad.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IQueryManager _queryManager;
        
        public MainWindowViewModel()
        {
        }
        
        public MainWindowViewModel(IQueryManager queryManager, ISession session, QueriesViewModel queriesViewModel)
        {
            Session = session;
            Queries = queriesViewModel;
            _queryManager = queryManager;
            
            // _openQueries = session.OpenQueries
            //     .ToObservableChangeSet().ToCollection()
            //     .ToProperty(this, x => x.OpenQueries);
            // _openQueries.ThrownExceptions.Subscribe(ex =>
            // {
            //     Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            // });
        }
        
        // private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Query>> _openQueries;
        // public IReadOnlyCollection<Query> OpenQueries => _openQueries.Value;
        
        public ISession Session { get; }
        public QueriesViewModel Queries { get; }
    }
}