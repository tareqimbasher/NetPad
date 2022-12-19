namespace NetPad.IO;

/// <summary>
/// Handles script out on multiple channels.
/// </summary>
public interface IScriptOutputAdapter
{
    IOutputWriter<object> ResultsChannel { get; }
    IOutputWriter<object>? SqlChannel { get; }
}

/// <summary>
/// Handles script out on multiple channels.
/// </summary>
/// <typeparam name="TResultsChannelOutput">The output type of the results channel.</typeparam>
/// <typeparam name="TSqlChannelOutput">The output type of the sql channel.</typeparam>
public interface IScriptOutputAdapter<in TResultsChannelOutput, in TSqlChannelOutput> : IScriptOutputAdapter
{
    new IOutputWriter<TResultsChannelOutput> ResultsChannel { get; }
    new IOutputWriter<TSqlChannelOutput>? SqlChannel { get; }

    public static ScriptOutputAdapter<TResultsChannelOutput, TSqlChannelOutput> Null { get; } = new(ActionOutputWriter<TResultsChannelOutput>.Null);
}

/// <summary>
/// A base class for an object that wants to implement the IScriptOutputAdapter<![CDATA[<T,T>]]> interface.
/// </summary>
/// <typeparam name="TResultsChannelOutput">The output type of the results channel.</typeparam>
/// <typeparam name="TSqlChannelOutput">The output type of the sql channel.</typeparam>
public record ScriptOutputAdapter<TResultsChannelOutput, TSqlChannelOutput>(
    IOutputWriter<TResultsChannelOutput> ResultsChannel,
    IOutputWriter<TSqlChannelOutput>? SqlChannel = null) : IScriptOutputAdapter<TResultsChannelOutput, TSqlChannelOutput>
{
    private IOutputWriter<object>? _rawResultsChannel;
    private IOutputWriter<object>? _rawSqlChannel;

    IOutputWriter<object> IScriptOutputAdapter.ResultsChannel =>
        _rawResultsChannel ??= new ActionOutputWriter<object>((o, title) => ResultsChannel.WriteAsync((TResultsChannelOutput?)o, title));

    IOutputWriter<object>? IScriptOutputAdapter.SqlChannel =>
        _rawSqlChannel ??= new ActionOutputWriter<object>((o, title) => SqlChannel?.WriteAsync((TSqlChannelOutput?)o, title));
}
