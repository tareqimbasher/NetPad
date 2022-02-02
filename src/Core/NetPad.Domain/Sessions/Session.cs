using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly IScriptEnvironmentFactory _scriptEnvironmentFactory;
        private readonly ILogger<Session> _logger;
        private readonly ObservableCollection<ScriptEnvironment> _environments;
        private ScriptEnvironment? _active;
        private Guid? _lastActiveScriptId = null;

        public Session(IScriptEnvironmentFactory scriptEnvironmentFactory, ILogger<Session> logger)
        {
            _scriptEnvironmentFactory = scriptEnvironmentFactory;
            _logger = logger;
            _environments = new ObservableCollection<ScriptEnvironment>();
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public ObservableCollection<ScriptEnvironment> Environments => _environments;

        public ScriptEnvironment? Active
        {
            get => _active;
            set => this.RaiseAndSetIfChanged(ref _active, value);
        }

        public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }


        public ScriptEnvironment? Get(Guid scriptId)
        {
            return _environments.FirstOrDefault(m => m.Script.Id == scriptId);
        }

        public async Task OpenAsync(Script script)
        {
            var environment = Get(script.Id);
            if (environment == null)
            {
                environment = await _scriptEnvironmentFactory.CreateEnvironmentAsync(script);
                _environments.Add(environment);
            }

            await ActivateAsync(script.Id);
        }

        public async Task CloseAsync(Guid scriptId)
        {
            _logger.LogDebug($"Closing script: {scriptId}");
            var environmentToClose = Get(scriptId);
            if (environmentToClose == null)
                return;

            var ix = _environments.IndexOf(environmentToClose);

            _environments.Remove(environmentToClose);
            environmentToClose.Dispose();

            if (Active == environmentToClose)
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
                    await ActivateAsync(null);
            }

            _logger.LogDebug($"Closed script: {scriptId}");
        }

        public Task ActivateAsync(Guid? scriptId)
        {
            _lastActiveScriptId = Active?.Script.Id;

            if (scriptId == null)
                Active = null;
            else
            {
                var environment = Get(scriptId.Value);
                Active = environment ?? throw new EnvironmentNotFoundException(scriptId.Value);
            }

            return Task.CompletedTask;
        }

        public Task ActivateLastActiveScriptAsync()
        {
            if (CanActivateLastActiveScript())
                ActivateAsync(_lastActiveScriptId);

            return Task.CompletedTask;
        }

        public Task<string> GetNewScriptNameAsync()
        {
            const string baseName = "Script";
            int number = 1;

            while (_environments.Any(m => m.Script.Name == $"{baseName} {number}"))
            {
                number++;
            }

            return Task.FromResult($"{baseName} {number}");
        }

        private bool CanActivateLastActiveScript() => _lastActiveScriptId != null && _environments.Any(e => e.Script.Id == _lastActiveScriptId);
    }
}
