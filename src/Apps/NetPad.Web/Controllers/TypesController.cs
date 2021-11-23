using Microsoft.AspNetCore.Mvc;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("types")]
    public class TypesController : Controller
    {
        [HttpGet]
        public (
            Script,
            ScriptPropertyChanged,
            ScriptOutputEmitted,
            EnvironmentsAdded,
            EnvironmentsRemoved,
            EnvironmentPropertyChanged,
            ActiveEnvironmentChanged
        )? GetAllTypes() => null;
    }
}
