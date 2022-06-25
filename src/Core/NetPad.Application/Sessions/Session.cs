using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly IScriptEnvironmentFactory _scriptEnvironmentFactory;
        private readonly IEventBus _eventBus;
        private readonly ILogger<Session> _logger;
        private readonly List<ScriptEnvironment> _environments;
        private Guid? _lastActiveScriptId;

        public Session(
            IScriptEnvironmentFactory scriptEnvironmentFactory,
            IEventBus eventBus,
            ILogger<Session> logger)
        {
            _scriptEnvironmentFactory = scriptEnvironmentFactory;
            _eventBus = eventBus;
            _logger = logger;
            _environments = new List<ScriptEnvironment>();
        }

        public IReadOnlyList<ScriptEnvironment> Environments => _environments.AsReadOnly();

        public ScriptEnvironment? Active { get; private set; }


        public ScriptEnvironment? Get(Guid scriptId)
        {
            return _environments.FirstOrDefault(m => m.Script.Id == scriptId);
        }

        public async Task OpenAsync(Script script, bool activate = true)
        {
            var environment = Get(script.Id);

            if (environment == null)
            {
                environment = await _scriptEnvironmentFactory.CreateEnvironmentAsync(script);
                _environments.Add(environment);
                await _eventBus.PublishAsync(new EnvironmentsAdded(environment));
            }

            if (activate)
            {
                await ActivateAsync(script.Id);
            }
        }

        public async Task OpenAsync(IEnumerable<Script> scripts)
        {
            Script? last = null;

            foreach (var script in scripts)
            {
                await OpenAsync(script, activate: false);
                last = script;
            }

            if (last != null)
            {
                await ActivateAsync(last.Id);
            }
        }

        public async Task CloseAsync(Guid scriptId, bool activateNextScript = true)
        {
            _logger.LogDebug("Closing script: {ScriptId}", scriptId);
            var environmentToClose = Get(scriptId);
            if (environmentToClose == null)
                return;

            var ix = _environments.IndexOf(environmentToClose);

            _environments.Remove(environmentToClose);
            await environmentToClose.DisposeAsync();
            await _eventBus.PublishAsync(new EnvironmentsRemoved(environmentToClose));

            if (activateNextScript && Active == environmentToClose)
            {
                if (_environments.Any())
                {
                    if (CanActivateLastActiveScript())
                        await ActivateAsync(_lastActiveScriptId);
                    else
                    {
                        ix = ix == 0 ? 1 : (ix - 1);
                        await ActivateAsync(_environments[ix].Script.Id);
                    }
                }
                else
                {
                    await ActivateAsync(null);
                }
            }

            _logger.LogDebug("Closed script: {ScriptId}", scriptId);
        }

        public async Task CloseAsync(IEnumerable<Guid> scriptIds)
        {
            var ids = scriptIds.ToArray();

            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];

                bool isLastScriptInBatch = i == (ids.Length - 1);

                await CloseAsync(id, activateNextScript: isLastScriptInBatch);
            }
        }

        public Task ActivateAsync(Guid? scriptId)
        {
            ScriptEnvironment? newActive;
            _lastActiveScriptId = Active?.Script.Id;

            if (scriptId == null)
                newActive = null;
            else
            {
                var environment = Get(scriptId.Value);
                newActive = environment ?? throw new EnvironmentNotFoundException(scriptId.Value);
            }

            Active = newActive;
            _eventBus.PublishAsync(new ActiveEnvironmentChanged(newActive?.Script.Id));

            return Task.CompletedTask;
        }

        public Task ActivateLastActiveScriptAsync()
        {
            if (CanActivateLastActiveScript())
                ActivateAsync(_lastActiveScriptId);

            return Task.CompletedTask;
        }

        private bool CanActivateLastActiveScript() => _lastActiveScriptId != null && _environments.Any(e => e.Script.Id == _lastActiveScriptId);
    }
}
