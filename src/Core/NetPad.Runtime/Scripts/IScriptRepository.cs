namespace NetPad.Scripts;

public interface IScriptRepository
{
    Task<IEnumerable<ScriptSummary>> GetAllAsync();
    Task<Script> CreateAsync(string name);
    Task<Script> GetAsync(string path);
    Task<Script?> GetAsync(Guid scriptId);
    Task<Script> SaveAsync(Script script);
    void Rename(Script script, string newName);
    Task DeleteAsync(Script script);
}
