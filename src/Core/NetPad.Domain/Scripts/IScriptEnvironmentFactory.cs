using System.Threading.Tasks;

namespace NetPad.Scripts
{
    public interface IScriptEnvironmentFactory
    {
        Task<ScriptEnvironment> CreateEnvironmentAsync(Script script);
    }
}
