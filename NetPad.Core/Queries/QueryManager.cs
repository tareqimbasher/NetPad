using System.IO;
using System.Threading.Tasks;
using NetPad.Sessions;

namespace NetPad.Queries
{
    public class QueryManager : IQueryManager
    {
        private readonly Settings _settings;
        private readonly Session _session;

        public QueryManager(Settings settings, Session session)
        {
            _settings = settings;
            _session = session;
        }
        
        public async Task<Query> CreateNewQueryAsync()
        {
            var directory = GetQueriesDirectory();
            var path = Path.Combine(directory.FullName, GetNewQueryName());

            var query = new Query(path);
            await query.SaveAsync();

            _session.Add(query);

            return query;
        }

        public async Task<Query> OpenQueryAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} was not found.");

            var query = new Query(filePath);
            await query.LoadAsync();
            
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

        private DirectoryInfo GetQueriesDirectory()
        {
            var directory = new DirectoryInfo(_settings.QueriesDirectoryPath);
            
            directory.Create();

            return directory;
        }
        
        private string GetNewQueryName()
        {
            var directory = GetQueriesDirectory();
            string baseName = "Query";
            int number = 1;
            
            while (File.Exists(Path.Combine(directory.FullName, $"{baseName} {number}.netpad")))
            {
                number++;
            }

            return $"{baseName} {number}.netpad";
        }
    }
}