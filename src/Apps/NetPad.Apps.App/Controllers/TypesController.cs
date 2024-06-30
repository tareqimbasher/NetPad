using Microsoft.AspNetCore.Mvc;
using NetPad.Application.Events;
using NetPad.Apps.CQs;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.UiInterop;
using NetPad.Configuration.Events;
using NetPad.Data.Events;
using NetPad.Dtos;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions.Events;

namespace NetPad.Controllers;

[ApiController]
[Route("types")]
public class TypesController : ControllerBase
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

        public IpcMessageBatch? IpcMessageBatch { get; set; }
        public ErrorResult? ErrorResult { get; set; }
        public Script? Script { get; set; }
        public HtmlResultsScriptOutput? HtmlResultsScriptOutput { get; set; }
        public HtmlErrorScriptOutput? HtmlErrorScriptOutput { get; set; }
        public HtmlRawScriptOutput? HtmlRawScriptOutput { get; set; }
        public HtmlSqlScriptOutput? HtmlSqlScriptOutput { get; set; }
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
        public DataConnectionSavedEvent? DataConnectionSavedEvent { get; set; }
        public DataConnectionDeletedEvent? DataConnectionDeletedEvent { get; set; }
        public DataConnectionResourcesUpdatingEvent? DataConnectionResourcesUpdatingEvent { get; set; }
        public DataConnectionResourcesUpdatedEvent? DataConnectionResourcesUpdatedEvent { get; set; }
        public DataConnectionResourcesUpdateFailedEvent? DataConnectionResourcesUpdateFailedEvent { get; set; }
        public DataConnectionSchemaValidationStartedEvent? DataConnectionSchemaValidationStartedEvent { get; set; }
        public DataConnectionSchemaValidationCompletedEvent? DataConnectionSchemaValidationCompletedEvent { get; set; }
        public OpenWindowCommand? OpenWindowCommand { get; set; }
        public ConfirmSaveCommand? ConfirmSaveCommand { get; set; }
        public RequestNewScriptNameCommand? RequestNewScriptNameCommand { get; set; }
        public AlertUserCommand? AlertUserCommand { get; set; }
        public ConfirmWithUserCommand? ConfirmWithUserCommand { get; set; }
        public PromptUserCommand? PromptUserCommand { get; set; }
        public PromptUserForInputCommand? PromptUserForInputCommand { get; set; }
        public AlertUserAboutMissingAppDependencies? AlertUserAboutMissingAppDependencies { get; set; }
        public MsSqlServerDatabaseConnection? MsSqlServerDatabaseConnection { get; set; }
        public PostgreSqlDatabaseConnection? PostgreSqlDatabaseConnection { get; set; }
        public SQLiteDatabaseConnection? SQLiteDatabaseConnection { get; set; }
    }
}
