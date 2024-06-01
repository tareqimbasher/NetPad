using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NetPad.Tests.Logging;

internal sealed class XUnitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(testOutputHelper, _scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
