using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NetPad.CQs;
using NetPad.Data;
using NetPad.UiInterop;

namespace NetPad.Controllers;

[ApiController]
[Route("data-connections")]
public class DataConnectionsController : Controller
{
    private readonly IMediator _mediator;

    public DataConnectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("open")]
    public async Task OpenDataConnectionWindow([FromServices] IUiWindowService uiWindowService, [FromQuery] Guid? dataConnectionId = null)
    {
        await uiWindowService.OpenDataConnectionWindowAsync(dataConnectionId);
    }

    [HttpGet]
    public async Task<IEnumerable<DataConnection>> GetAll() => await _mediator.Send(new GetDataConnectionsQuery());

    [HttpGet("names")]
    public async Task<IEnumerable<string>> GetAllNames()
    {
        var dataConnections = await GetAll();
        return dataConnections.Select(c => c.Name).ToArray();
    }

    [HttpPut]
    public async Task Save(DataConnection dataConnection) => await _mediator.Send(new SaveDataConnectionCommand(dataConnection));

    [HttpDelete("{id:guid}")]
    public async Task Delete(Guid id) => await _mediator.Send(new DeleteDataConnectionCommand(id));

    [HttpPatch("test")]
    public async Task<DataConnectionTestResult> Test([FromBody] DataConnection dataConnection) => await dataConnection.TestConnectionAsync();

    [HttpPatch("databases")]
    public async Task<IEnumerable<string>> GetDatabases([FromBody] DataConnection dataConnection)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection dbConnection)
        {
            throw new InvalidOperationException("Cannot get databases except on Entity Framework database connections.");
        }

        return await dbConnection.GetDatabasesAsync();
    }

    [HttpPatch("{id:guid}/database-structure")]
    public async Task<DatabaseStructure> GetDatabaseStructure(Guid id)
    {
        var dataConnection = await _mediator.Send(new GetDataConnectionQuery(id));

        if (dataConnection is not DatabaseConnection databaseConnection)
        {
            throw new InvalidOperationException($"Cannot get database structure except on connections of type {nameof(DatabaseConnection)}.");
        }

        return await _mediator.Send(new GetDatabaseConnectionStructureQuery(databaseConnection));
    }
}
