/// <summary>
/// Meant to be used as script code when language is SQL. This code provides the boilerplate
/// that runs user's SQL code.
///
/// This is embedded into the assembly to be read later as an Embedded Resource.
/// </summary>

await using var command = DataContext.Database.GetDbConnection().CreateCommand();

command.CommandText = @"SQL_CODE";
await DataContext.Database.OpenConnectionAsync();

try
{
    await using var reader = await command.ExecuteReaderAsync();

    do
    {
        var dataTable = new System.Data.DataTable();
        dataTable.Load(reader);

        if (dataTable.Rows.Count > 0)
        {
            dataTable.Dump();
        }
        else
        {
            "No rows returned".Dump();
        }
    } while (!reader.IsClosed);

    return 0;
}
catch (System.Exception ex)
{
    ex.Message.Dump();
    return 1;
}
