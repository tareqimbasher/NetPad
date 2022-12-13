using Microsoft.AspNetCore.Mvc;
using NetPad.Plugins.OmniSharp.Events;

namespace NetPad.Plugins.OmniSharp;

[ApiController]
[Route("omnisharp/types")]
public class TypesController : Controller
{
    // This endpoint was created for the sole reason of providing Swagger with types that are not exposed by other endpoints
    // that we want in the generated code
    [ProducesResponseType(typeof(Types), 200)]
    [HttpGet]
    public void AdditionalTypes()
    {
    }

    private class Types
    {
        public OmniSharpDiagnosticsEvent? OmniSharpDiagnosticsEvent { get; set; }
        public OmniSharpAsyncBufferUpdateCompletedEvent? OmniSharpAsyncBufferUpdateCompletedEvent { get; set; }
    }
}
