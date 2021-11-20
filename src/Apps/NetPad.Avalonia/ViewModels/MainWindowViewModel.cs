using NetPad.Queries;
using NetPad.Sessions;
using NetPad.ViewModels.Queries;

namespace NetPad.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IQueryRepository _queryRepository;
        
        public MainWindowViewModel()
        {
        }
        
        public MainWindowViewModel(IQueryRepository queryRepository, ISession session, QueriesViewModel queriesViewModel)
        {
            Session = session;
            Queries = queriesViewModel;
            _queryRepository = queryRepository;
            
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