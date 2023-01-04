using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Tests.Logging;
using Xunit.Abstractions;

namespace NetPad.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly ITestOutputHelper _testOutputHelper;

    public TestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile("appsettings.Local.json", true)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(config =>
        {
            config.AddProvider(new XUnitLoggerProvider(testOutputHelper));
            config.AddConfiguration(configuration.GetSection("Logging"));
        });

        services.AddSingleton<IEventBus, EventBus>();

        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider(true);

        Logger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType().FullName!);
    }

    protected ServiceProvider ServiceProvider { get; }
    protected ILogger Logger { get; }

    protected virtual void ConfigureServices(ServiceCollection services)
    {
    }


    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServiceProvider.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
