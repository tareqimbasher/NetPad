using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetPad.Apps.CQs;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Apps.UiInterop;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Security;
using NetPad.DotNet;

namespace NetPad.Controllers;

[ApiController]
[Route("data-connections")]
public class DataConnectionsController(IMediator mediator) : ControllerBase
{
    [HttpPatch("open")]
    public async Task OpenDataConnectionWindow(
        [FromServices] IUiWindowService uiWindowService,
        [FromQuery] Guid? dataConnectionId = null,
        [FromQuery] bool copy = false)
    {
        await uiWindowService.OpenDataConnectionWindowAsync(dataConnectionId, copy);
    }

    [HttpGet]
    public async Task<IEnumerable<DataConnection>> GetAll() => await mediator.Send(new GetDataConnectionsQuery());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataConnection?>> Get(Guid id)
    {
        var connection = await mediator.Send(new GetDataConnectionQuery(id));

        if (connection == null) return connection;

        // TODO find out why this happens and fix
        // Manually serializing the result because JsonConverter on DataConnection
        // isn't triggered when serializing a single object. It works on collections though.
        var json = JsonSerializer.Serialize(connection, typeof(DataConnection));

        return new ContentResult
        {
            StatusCode = 200,
            Content = json,
            ContentType = "application/json"
        };
    }

    [HttpGet("names")]
    public async Task<IEnumerable<string>> GetAllNames()
    {
        var dataConnections = await GetAll();
        return dataConnections.Select(c => c.Name).ToArray();
    }

    [HttpPut]
    public async Task Save(DataConnection dataConnection) =>
        await mediator.Send(new SaveDataConnectionCommand(dataConnection));

    [HttpPatch("{id:guid}/refresh")]
    public async Task Refresh(Guid id) => await mediator.Send(new RefreshDataConnectionCommand(id));

    [HttpDelete("{id:guid}")]
    public async Task Delete(Guid id) => await mediator.Send(new DeleteDataConnectionCommand(id));

    [HttpPost("connection-string")]
    public string GetConnectionString([FromBody] DataConnection dataConnection)
    {
        return dataConnection is not DatabaseConnection dbConnection
            ? string.Empty
            : dbConnection.GetConnectionString(new FakeDataConnectionPasswordProtector());
    }

    [HttpPatch("test")]
    public async Task<DataConnectionTestResult> Test(
        [FromBody] DataConnection dataConnection,
        [FromServices] IDataConnectionPasswordProtector passwordProtector) =>
        await dataConnection.TestConnectionAsync(passwordProtector);

    [HttpPatch("protect-password")]
    public string? ProtectPassword(
        [FromBody] string unprotectedPassword,
        [FromServices] IDataConnectionPasswordProtector passwordProtector) =>
        passwordProtector.Protect(unprotectedPassword);

    [HttpPatch("databases")]
    public async Task<IEnumerable<string>> GetDatabases(
        [FromBody] DataConnection dataConnection,
        [FromServices] IDataConnectionPasswordProtector passwordProtector)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection dbConnection)
        {
            throw new InvalidOperationException(
                "Cannot get databases except on Entity Framework database connections.");
        }

        return await dbConnection.GetDatabasesAsync(passwordProtector);
    }

    [HttpPatch("{id:guid}/database-structure")]
    public async Task<DatabaseStructure> GetDatabaseStructure(Guid id)
    {
        var dataConnection = await mediator.Send(new GetDataConnectionQuery(id));

        if (dataConnection is not DatabaseConnection databaseConnection)
        {
            throw new InvalidOperationException(
                $"Cannot get database structure except on connections of type {nameof(DatabaseConnection)}.");
        }

        return await mediator.Send(new GetDatabaseConnectionStructureQuery(databaseConnection));
    }

    [HttpPatch("{id:guid}/scaffold-to-project")]
    public async Task<IActionResult> ScaffoldToProject(
        Guid id,
        string projectDirectoryPath,
        DotNetFrameworkVersion frameworkVersion,
        [FromServices] IDataConnectionPasswordProtector dataConnectionPasswordProtector,
        [FromServices] IDotNetInfo dotNetInfo,
        [FromServices] Settings settings,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var dataConnection = await mediator.Send(new GetDataConnectionQuery(id));

        if (dataConnection is not DatabaseConnection databaseConnection)
        {
            throw new InvalidOperationException(
                $"Cannot get database structure except on connections of type {nameof(DatabaseConnection)}.");
        }

        var scaffolder = new EntityFrameworkDatabaseScaffolder(
            dataConnectionPasswordProtector,
            dotNetInfo,
            settings,
            loggerFactory.CreateLogger<EntityFrameworkDatabaseScaffolder>());

        var project = await scaffolder.ScaffoldToProjectAsync(
            projectDirectoryPath,
            "DataConnection",
            frameworkVersion,
            (EntityFrameworkDatabaseConnection)databaseConnection);

        // Switch project to a executable project
        await project.SetProjectPropertyAsync(
            "OutputType",
            ProjectOutputType.Executable.ToDotNetProjectPropertyValue());

        var programFilePath = Path.Combine(project.ProjectDirectoryPath, "Program.cs");
        if (!System.IO.File.Exists(programFilePath))
        {
            await System.IO.File.WriteAllTextAsync(
                programFilePath,
                """
                using Microsoft.EntityFrameworkCore;

                var dbContext = new GeneratedDbContext();
                var connectionString = dbContext.Database.GetConnectionString();
                Console.WriteLine($"Connection string:\n{connectionString}");
                """
            );
        }

        ProcessUtil.OpenWithDefaultApp(project.ProjectDirectoryPath);
        return NoContent();
    }
}
