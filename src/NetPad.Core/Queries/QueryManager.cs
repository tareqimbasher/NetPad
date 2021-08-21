using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NetPad.Sessions;

namespace NetPad.Queries
{
    public class QueryManager : IQueryManager
    {
        private readonly Settings _settings;
        private readonly ISession _session;

        public QueryManager(Settings settings, ISession session)
        {
            _settings = settings;
            _session = session;
        }
        
        public Task<Query> CreateNewQueryAsync()
        {
            var query = new Query(GetNewQueryName());

            _session.Add(query);

            return Task.FromResult(query);
        }

        public async Task<Query> OpenQueryAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File {filePath} was not found.");

            var query = new Query(fileInfo);
            await query.LoadAsync().ConfigureAwait(false);
            
            _session.Add(query);

            return query;
        }

        public Task<Query> DuplicateQueryAsync(Query query, QueryDuplicationOptions options)
        {
            throw new System.NotImplementedException();
        }

        public Task<Query> DeleteQueryAsync(Query query)
        {
            throw new System.NotImplementedException();
        }

        public Task<Query> CloseQueryAsync(Query query)
        {
            _session.Remove(query);
            return Task.FromResult(query);
        }

        public Task<DirectoryInfo> GetQueriesDirectoryAsync()
        {
            var directory = new DirectoryInfo(_settings.QueriesDirectoryPath);
            
            directory.Create();

            return Task.FromResult(directory);
        }
        
        private string GetNewQueryName()
        {
            const string baseName = "Query";
            int number = 1;
            
            while (_session.OpenQueries.Any(q => q.Name == $"{baseName} {number}"))
            {
                number++;
            }

            return $"{baseName} {number}.netpad";
        }
    }
}