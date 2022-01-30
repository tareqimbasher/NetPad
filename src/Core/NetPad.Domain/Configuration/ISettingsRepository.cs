using System.Threading.Tasks;

namespace NetPad.Configuration
{
    public interface ISettingsRepository
    {
        Task<Settings> GetSettingsAsync();
        Task SaveSettingsAsync(Settings settings);
    }
}
