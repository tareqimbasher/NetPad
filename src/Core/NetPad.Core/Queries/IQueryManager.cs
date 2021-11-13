using System;
using System.IO;
using System.Threading.Tasks;

namespace NetPad.Queries
{
    public interface IQueryManager
    {
        Task<Query> CreateNewQueryAsync();
        Task<Query> OpenQueryAsync(string filePath);
        Task<Query> DuplicateQueryAsync(Query query, QueryDuplicationOptions options);
        Task<Query> SaveQueryAsync(Query query);
        Task<Query> DeleteQueryAsync(Query query);
        Task CloseQueryAsync(Guid id);
        Task<DirectoryInfo> GetQueriesDirectoryAsync();
    }
}
