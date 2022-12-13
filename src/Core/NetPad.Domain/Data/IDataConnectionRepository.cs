using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPad.Data;

public interface IDataConnectionRepository
{
    Task<IEnumerable<DataConnection>> GetAllAsync();
    Task<DataConnection?> GetAsync(Guid id);
    Task SaveAsync(DataConnection connection);
    Task DeleteAsync(Guid id);
}
