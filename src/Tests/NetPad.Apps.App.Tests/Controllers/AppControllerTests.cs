using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetPad.Controllers;

namespace NetPad.Apps.App.Tests.Controllers;

public class AppControllerTests
{
    [Fact]
    public void SendRemoteLog_RemovesLineBreaksFromMessage()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var controller = new AppController(NullLogger<AppController>.Instance);

        controller.SendRemoteLog(
            AppController.LogSource.WebApp,
            [
                new AppController.RemoteLogMessage
                {
                    Logger = "my-logger",
                    LogLevel = LogLevel.Information,
                    Message = "real entry\r\nfail: forged entry",
                    Date = DateTime.UtcNow
                }
            ],
            loggerFactory);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.DoesNotContain('\n', entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.Contains("real entry", entry.Message);
        Assert.Contains("forged entry", entry.Message);
    }

    [Fact]
    public void SendRemoteLog_RemovesControlCharactersFromOptionalParams()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var controller = new AppController(NullLogger<AppController>.Instance);

        controller.SendRemoteLog(
            AppController.LogSource.WebApp,
            [
                new AppController.RemoteLogMessage
                {
                    Logger = "my-logger",
                    LogLevel = LogLevel.Error,
                    Message = "message",
                    OptionalParams = ["param\r\nwarn: forged"],
                    Date = DateTime.UtcNow
                }
            ],
            loggerFactory);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.DoesNotContain('\n', entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
    }

    [Fact]
    public void SendRemoteLog_DoesNotTreatBracesInMessageAsPlaceholders()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var controller = new AppController(NullLogger<AppController>.Instance);

        controller.SendRemoteLog(
            AppController.LogSource.WebApp,
            [
                new AppController.RemoteLogMessage
                {
                    Logger = "my-logger",
                    LogLevel = LogLevel.Information,
                    Message = "value is {NotAPlaceholder}",
                    Date = DateTime.UtcNow
                }
            ],
            loggerFactory);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal("value is {NotAPlaceholder}", entry.Message);
    }

    [Fact]
    public void SendRemoteLog_TruncatesOverlyLongMessages()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var controller = new AppController(NullLogger<AppController>.Instance);

        controller.SendRemoteLog(
            AppController.LogSource.WebApp,
            [
                new AppController.RemoteLogMessage
                {
                    Logger = "my-logger",
                    LogLevel = LogLevel.Information,
                    Message = new string('a', 10_000),
                    Date = DateTime.UtcNow
                }
            ],
            loggerFactory);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.True(entry.Message.Length < 10_000);
        Assert.EndsWith("... [truncated]", entry.Message);
    }

    [Theory]
    [InlineData("has space", "RemoteLog.WebApp.has_space")]
    [InlineData("new\r\nline", "RemoteLog.WebApp.new__line")]
    [InlineData(null, "RemoteLog.WebApp.unknown-logger")]
    [InlineData("", "RemoteLog.WebApp.unknown-logger")]
    [InlineData("valid.logger-name_1", "RemoteLog.WebApp.valid.logger-name_1")]
    public void SendRemoteLog_SanitizesLoggerName(string? loggerName, string expectedCategory)
    {
        var loggerFactory = new CapturingLoggerFactory();
        var controller = new AppController(NullLogger<AppController>.Instance);

        controller.SendRemoteLog(
            AppController.LogSource.WebApp,
            [
                new AppController.RemoteLogMessage
                {
                    Logger = loggerName,
                    LogLevel = LogLevel.Information,
                    Message = "message",
                    Date = DateTime.UtcNow
                }
            ],
            loggerFactory);

        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(expectedCategory, entry.Category);
    }

    private record LogEntry(string Category, LogLevel LogLevel, string Message);

    private class CapturingLoggerFactory : ILoggerFactory
    {
        public List<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    private class CapturingLogger(string category, List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(new LogEntry(category, logLevel, formatter(state, exception)));
        }
    }
}
