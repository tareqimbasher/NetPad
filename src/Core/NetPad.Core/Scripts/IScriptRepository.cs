using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPad.Scripts
{
    public interface IScriptRepository
    {
        Task<List<ScriptSummary>> GetAllAsync();
        Task<Script> CreateAsync();
        Task<Script> OpenAsync(string filePath);
        Task CloseAsync(Guid id);
        Task<Script> DuplicateAsync(Script script, ScriptDuplicationOptions options);
        Task<Script> SaveAsync(Script script);
        Task<Script> DeleteAsync(Script script);
    }
}
