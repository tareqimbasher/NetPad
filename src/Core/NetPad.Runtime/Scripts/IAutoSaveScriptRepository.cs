namespace NetPad.Scripts;

/// <summary>
/// Persists and retrieves auto-saved scripts.
/// </summary>
public interface IAutoSaveScriptRepository
{
    Task<Script?> GetScriptAsync(Guid scriptId);
    Task<List<Script>> GetScriptsAsync();
    Task<Script> SaveAsync(Script script);
    Task DeleteAsync(Script script);
}
