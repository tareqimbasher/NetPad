using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Session> _logger;
        private readonly ObservableCollection<ScriptEnvironment> _environments;
        private ScriptEnvironment? _active;
        private Guid? _lastActiveScriptId = null;

        public Session(IServiceProvider serviceProvider, ILogger<Session> logger)
        {
            _serviceProvider = serviceProvider;
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
                environment = new ScriptEnvironment(script, _serviceProvider.CreateScope());
                _environments.Add(environment);
            }

            await ActivateAsync(script.Id);
        }

        public async Task CloseAsync(Guid scriptId)
        {
            _logger.LogDebug($"Closing script: {scriptId}");
            var environment = Get(scriptId);
            if (environment == null)
                return;

            var ix = _environments.IndexOf(environment);

            _environments.Remove(environment);
            environment.Dispose();

            if (Active == environment)
            {
                if (_environments.Any())
                {
                    if (ix != 0)
                        ix--;
                    await ActivateAsync(_environments[ix].Script.Id);
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
            if (_lastActiveScriptId != null)
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
    }
}
