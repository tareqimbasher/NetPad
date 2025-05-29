using NetPad.Scripts;

namespace NetPad.Data;

public interface IExtensionsCodeProvider
{
    Task<IEnumerable<Script>> GetAll(Guid currentScriptId);
}
