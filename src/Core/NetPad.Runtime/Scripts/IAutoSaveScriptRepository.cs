namespace NetPad.Scripts;

public interface IAutoSaveScriptRepository
{
    Task<Script?> GetScriptAsync(Guid scriptId);
    Task<IEnumerable<Script>> GetScriptsAsync();
    Task<Script> SaveAsync(Script script);
    Task DeleteAsync(Script script);
}
