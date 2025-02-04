using NetPad.ExecutionModel.ClientServer.ScriptServices;

namespace NetPad.ExecutionModel.ClientServer.Messages;

public record MemCacheItemInfoChangedMessage(MemCacheItemInfo[] Items);
