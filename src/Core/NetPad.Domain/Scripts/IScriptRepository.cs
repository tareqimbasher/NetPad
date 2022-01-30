using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPad.Scripts
{
    public interface IScriptRepository
    {
        Task<List<ScriptSummary>> GetAllAsync();
        Task<Script> CreateAsync(string name);
        Task<Script> GetAsync(string path);
        Task<Script> SaveAsync(Script script);
        Task DeleteAsync(Script script);
    }
}
