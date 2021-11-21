using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Sessions
{
    public class Session : ISession
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ObservableCollection<ScriptEnvironment> _environments;
        private ScriptEnvironment? _active;

        public Session(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

            await SetActive(script.Id);
        }

        public async Task CloseAsync(Guid scriptId)
        {
            var environment = Get(scriptId);
            if (environment != null)
            {
                _environments.Remove(environment);
                await environment.CloseAsync();
                environment.Dispose();

                if (Active == environment)
                {
                    await SetActive(null);
                }
            }
        }

        public Task SetActive(Guid? scriptId)
        {
            if (scriptId == null)
                Active = null;
            else
            {
                var environment = Get(scriptId.Value);
                Active = environment ?? throw new EnvironmentNotFoundException(scriptId.Value);
            }

            return Task.CompletedTask;
        }

        public Task<string> GetNewScriptName()
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
