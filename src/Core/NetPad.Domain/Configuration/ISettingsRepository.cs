using System.Threading.Tasks;
using NetPad.IO;

namespace NetPad.Configuration
{
    public interface ISettingsRepository
    {
        Task<FilePath> GetSettingsFileLocationAsync();
        Task<Settings> GetSettingsAsync();
        Task SaveSettingsAsync(Settings settings);
    }
}
