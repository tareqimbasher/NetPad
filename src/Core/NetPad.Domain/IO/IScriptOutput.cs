using System;
using System.Linq;

namespace NetPad.IO;

public interface IScriptOutput
{
    IOutputWriter PrimaryChannel { get; }
    IOutputWriter? SqlChannel { get; }
    public IOutputWriter[] AllChannels => new[] { PrimaryChannel, SqlChannel }.Where(c => c != null).ToArray()!;
    public static ScriptOutput Null { get; } = new ScriptOutput(ActionOutputWriter.Null);
}

public record ScriptOutput : IScriptOutput
{
    public ScriptOutput(IOutputWriter primaryChannel)
    {
        PrimaryChannel = primaryChannel;
    }

    public ScriptOutput(IOutputWriter primaryChannel, IOutputWriter sqlChannel) : this(primaryChannel)
    {
        SqlChannel = sqlChannel ?? throw new ArgumentNullException(nameof(sqlChannel));
    }

    public IOutputWriter PrimaryChannel { get; init; }
    public IOutputWriter? SqlChannel { get; init; }
}
