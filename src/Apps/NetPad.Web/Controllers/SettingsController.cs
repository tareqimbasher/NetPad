using Microsoft.AspNetCore.Mvc;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("settings")]
    public class SettingsController : Controller
    {
        private readonly Settings _settings;

        public SettingsController(Settings settings)
        {
            _settings = settings;
        }

        public Settings Index()
        {
            return _settings;
        }
    }
}
