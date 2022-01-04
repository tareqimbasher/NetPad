using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Sessions;

namespace NetPad.Tests.Helpers
{
    public static class SessionTestHelper
    {
        public static Session CreateSession(IServiceProvider serviceProvider)
        {
            return new Session(
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<Session>>());
        }
    }
}
