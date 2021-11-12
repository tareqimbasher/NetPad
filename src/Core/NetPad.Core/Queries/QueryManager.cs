using System;
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
            var query = new Query( Guid.NewGuid(), GetNewQueryName());

            _session.Add(query);

            return Task.FromResult(query);
        }

        public async Task<Query> OpenQueryAsync(string filePath)
        {
            var query = _session.Get(filePath);
            if (query != null)
                return query;

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File {filePath} was not found.");

            query = new Query(Guid.NewGuid(), fileInfo.Name);
            query.SetFilePath(filePath);
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

        public Task CloseQueryAsync(Guid id)
        {
            _session.Remove(id);
            return Task.CompletedTask;
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

            return $"{baseName} {number}";
        }
    }
}
