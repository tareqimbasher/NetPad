namespace NetPad.Apps.Mcp.Tests.Helpers;

internal static class ApiClientTestHelper
{
    public static readonly NetPadConnection TestConnection = new("http://localhost:5000", "test-token");

    public static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
    {
        var handler = new MockHttpMessageHandler();
        var client = new NetPadApiClient(TestConnection, handler);
        return (client, handler);
    }
}
