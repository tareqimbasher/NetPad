using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NetPad.Tests.Logging;

internal sealed class XUnitLogger<T>(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
    : XUnitLogger(testOutputHelper, scopeProvider, typeof(T).FullName ?? "Unknown_Type_Name"), ILogger<T>;

internal class XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
    : ILogger
{
    public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) =>
        new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");

    public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper) =>
        new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state) => scopeProvider.Push(state);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?,
            string> formatter)
    {
        var sb = new StringBuilder();
        sb.Append(GetLogLevelString(logLevel))
            .Append(" [").Append(categoryName).Append("] ")
            .Append(formatter(state, exception));

        if (exception != null)
        {
            sb.Append('\n').Append(exception);
        }

        // Append scopes
        scopeProvider.ForEachScope((scope, state) =>
        {
            state.Append("\n => ");
            state.Append(scope);
        }, sb);

        testOutputHelper.WriteLine(sb.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace =>       "trce",
            LogLevel.Debug =>       "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning =>     "warn",
            LogLevel.Error =>       "fail",
            LogLevel.Critical =>    "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}
