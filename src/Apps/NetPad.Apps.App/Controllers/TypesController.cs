using Microsoft.AspNetCore.Mvc;
using NetPad.CQs;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("types")]
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
            public YesNoCancel YesNoCancel { get; set; }

            public Script? Script { get; set; }
            public SettingsUpdatedEvent? SettingsUpdated { get; set; }
            public AppStatusMessagePublishedEvent? AppStatusMessagePublished { get; set; }
            public ScriptPropertyChangedEvent? ScriptPropertyChanged { get; set; }
            public ScriptConfigPropertyChangedEvent? ScriptConfigPropertyChanged { get; set; }
            public ScriptOutputEmittedEvent? ScriptOutputEmitted { get; set; }
            public EnvironmentsAddedEvent? EnvironmentsAdded { get; set; }
            public EnvironmentsRemovedEvent? EnvironmentsRemoved { get; set; }
            public EnvironmentPropertyChangedEvent? EnvironmentPropertyChanged { get; set; }
            public ActiveEnvironmentChangedEvent? ActiveEnvironmentChanged { get; set; }
            public ScriptDirectoryChangedEvent? ScriptDirectoryChanged { get; set; }
            public OpenWindowCommand? OpenWindowCommand { get; set; }
            public ConfirmSaveCommand? ConfirmSaveCommand { get; set; }
            public RequestNewScriptNameCommand? RequestNewScriptNameCommand { get; set; }
        }
    }
}
