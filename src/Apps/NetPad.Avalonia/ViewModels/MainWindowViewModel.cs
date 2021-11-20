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

            // _openScripts = session.OpenScripts
            //     .ToObservableChangeSet().ToCollection()
            //     .ToProperty(this, x => x.OpenScripts);
            // _openScripts.ThrownExceptions.Subscribe(ex =>
            // {
            //     Trace.TraceError($"TIPS-TRACE ERROR: {ex}");
            // });
        }

        // private readonly ObservableAsPropertyHelper<IReadOnlyCollection<Script>> _openScripts;
        // public IReadOnlyCollection<Script> OpenScripts => _openScripts.Value;

        public ISession Session { get; }
        public ScriptsViewModel Scripts { get; }
    }
}
