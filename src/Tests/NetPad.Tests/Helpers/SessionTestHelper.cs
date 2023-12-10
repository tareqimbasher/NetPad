using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.Tests.Services;

namespace NetPad.Tests.Helpers;

public static class SessionTestHelper
{
    public static Session CreateSession(IServiceProvider serviceProvider)
    {
        return new Session(
            new DefaultScriptEnvironmentFactory(serviceProvider),
            new NullTrivialDataStore(),
            serviceProvider.GetRequiredService<IEventBus>(),
            serviceProvider.GetRequiredService<ILogger<Session>>());
    }
}
