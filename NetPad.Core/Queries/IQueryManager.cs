using System.Threading.Tasks;

namespace NetPad.Queries
{
    public interface IQueryManager
    {
        Task<Query> CreateNewQueryAsync();
        Task<Query> OpenQueryAsync(string filePath);
        Task<Query> DuplicateQueryAsync(Query query, QueryDuplicationOptions options);
        Task<Query> DeleteQueryAsync(Query query);
        Task<Query> CloseQueryAsync(Query query);
    }
}