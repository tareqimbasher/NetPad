using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.Sessions;

namespace NetPad.Queries
{
    public class QueryRepository : IQueryRepository
    {
        private readonly Settings _settings;
        private readonly ISession _session;

        public QueryRepository(Settings settings, ISession session)
        {
            _settings = settings;
            _session = session;
        }

        public Task<List<QuerySummary>> GetAllAsync()
        {
            var summaries = Directory.GetFiles(_settings.QueriesDirectoryPath, "*.netpad", SearchOption.AllDirectories)
                .Select(f => new QuerySummary(Path.GetFileNameWithoutExtension(f), f))
                .ToList();

            return Task.FromResult(summaries);
        }

        public Task<Query> CreateAsync()
        {
            var query = new Query(Guid.NewGuid(), GetNewQueryName());

            _session.Add(query);

            return Task.FromResult(query);
        }

        public async Task<Query> OpenAsync(string filePath)
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

        public Task<Query> DuplicateAsync(Query query, QueryDuplicationOptions options)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Query> SaveAsync(Query query)
        {
            if (query.FilePath == null)
                throw new InvalidOperationException($"{nameof(query.FilePath)} is not set. Cannot save query.");

            var config = JsonSerializer.Serialize(query.Config);

            await File.WriteAllTextAsync(query.FilePath, $"{query.Id}\n" +
                                                         $"{config}\n" +
                                                         $"#Query\n" +
                                                         $"{query.Code}")
                .ConfigureAwait(false);

            query.IsDirty = false;
            return query;
        }

        public Task<Query> DeleteAsync(Query query)
        {
            throw new System.NotImplementedException();
        }

        public Task CloseAsync(Guid id)
        {
            _session.Remove(id);
            return Task.CompletedTask;
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
