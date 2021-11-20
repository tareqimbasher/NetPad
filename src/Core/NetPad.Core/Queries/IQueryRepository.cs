using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPad.Queries
{
    public interface IQueryRepository
    {
        Task<List<QuerySummary>> GetAllAsync();
        Task<Query> CreateAsync();
        Task<Query> OpenAsync(string filePath);
        Task CloseAsync(Guid id);
        Task<Query> DuplicateAsync(Query query, QueryDuplicationOptions options);
        Task<Query> SaveAsync(Query query);
        Task<Query> DeleteAsync(Query query);
    }
}
