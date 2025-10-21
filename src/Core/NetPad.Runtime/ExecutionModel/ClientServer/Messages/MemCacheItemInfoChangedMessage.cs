using NetPad.ExecutionModel.ScriptServices;

namespace NetPad.ExecutionModel.ClientServer.Messages;

public record MemCacheItemInfoChangedMessage(MemCacheItemInfo[] Items);
