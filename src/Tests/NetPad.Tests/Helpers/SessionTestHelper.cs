using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Sessions;
using NetPad.Tests.Services;

namespace NetPad.Tests.Helpers;

public static class SessionTestHelper
{
    public static Session CreateSession(IServiceProvider serviceProvider)
    {
        return new Session(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new NullTrivialDataStore(),
            serviceProvider.GetRequiredService<IEventBus>(),
            serviceProvider.GetRequiredService<ILogger<Session>>());
    }
}
