using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Scripts
{
    public interface IScriptEnvironmentFactory
    {
        Task<ScriptEnvironment> CreateEnvironmentAsync(Script script);
    }

    public class ScriptEnvironmentFactory : IScriptEnvironmentFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ScriptEnvironmentFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<ScriptEnvironment> CreateEnvironmentAsync(Script script)
        {
            var environment = new ScriptEnvironment(script, _serviceProvider.CreateScope());
            return Task.FromResult(environment);
        }
    }
}
