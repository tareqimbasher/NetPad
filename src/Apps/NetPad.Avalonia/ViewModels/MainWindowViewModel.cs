using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.ViewModels.Scripts;

namespace NetPad.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IScriptRepository _scriptRepository;

        public MainWindowViewModel()
        {
        }

        public MainWindowViewModel(IScriptRepository scriptRepository, ISession session, ScriptsViewModel scriptsViewModel)
        {
            Session = session;
            Scripts = scriptsViewModel;
            _scriptRepository = scriptRepository;

            // _openScripts = session.Scripts
            //     .ToObservableChangeSet().ToCollection()
            //     .ToProperty(this, x => x.Scripts);
            // _openScripts.ThrownExceptions.Subscribe(ex =>
            // {
            //     Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            // });
        }

        // private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Script>> _openScripts;
        // public IReadOnlyCollection<Script> Scripts => _openScripts.Value;

        public ISession Session { get; }
        public ScriptsViewModel Scripts { get; }
    }
}
