using NetPad.DotNet;

namespace NetPad.Scripts;

public interface IScriptRepository
{
    Task<IEnumerable<ScriptSummary>> GetAllAsync();
    Task<Script> CreateAsync(string name, DotNetFrameworkVersion targetFrameworkVersion);
    Task<Script> GetAsync(string path);
    Task<Script?> GetAsync(Guid scriptId);
    Task<List<Script>> GetAsync(HashSet<Guid> scriptIds);
    Task<Script> SaveAsync(Script script);
    void Rename(Script script, string newName);
    Task DeleteAsync(Script script);
}
