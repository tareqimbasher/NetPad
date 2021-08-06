using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.Exceptions;

namespace NetPad.Queries
{
    public class Query
    {
        public Query(string filePath)
        {
            FilePath = filePath;
            Config = new QueryConfig(); 
            Code = string.Empty;
        }

        public string FilePath { get; set; }
        public string Name => Path.GetFileNameWithoutExtension(FilePath);
        public string DirectoryPath => Path.GetDirectoryName(FilePath)!;
        
        public QueryConfig Config { get; private set; }
        public string Code { get; private set; }
        public bool IsDirty { get; private set; }


        public async Task LoadAsync()
        {
            var text = await File.ReadAllTextAsync(FilePath);

            var parts = text.Split("#Query");
            if (parts.Length != 2)
                throw new InvalidQueryFormat(FilePath);

            Config = JsonSerializer.Deserialize<QueryConfig>(parts[0]) ?? throw new InvalidQueryFormat(FilePath);
            Code = parts[1].TrimStart();
        }

        public async Task SaveAsync()
        {
            var config = JsonSerializer.Serialize(Config);

            await File.WriteAllTextAsync(FilePath, $"{config}\n" +
                                                   $"#Query\n" +
                                                   $"{Code}");
            IsDirty = false;
        }

        public async Task UpdateCodeAsync(string newCode)
        {
            Code = newCode ?? string.Empty;
            IsDirty = true;
        }
    }
}