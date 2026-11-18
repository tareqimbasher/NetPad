using NetPad.Plugins.OmniSharp.Services;

namespace NetPad.Plugins.OmniSharp.Tests.Services;

public class OmniSharpServerCatalogTests
{
    private const string GenericMessage = "OmniSharp server failed to start. Check log file for details.";

    [Fact]
    public void GetStartFailureMessage_NamesTheRuntimeToInstall_WhenTheRuntimeIsMissing()
    {
        var exception = new AggregateException(
            new OmniSharpRuntimeMissingException(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true)));

        Assert.Equal(
            "OmniSharp needs the .NET 10 runtime or newer. Install it, then restart NetPad.",
            OmniSharpServerCatalog.GetStartFailureMessage(exception));
    }

    [Fact]
    public void GetStartFailureMessage_IsGeneric_ForAnyOtherFailure()
    {
        var exception = new AggregateException(new InvalidOperationException("Process exited"));

        Assert.Equal(GenericMessage, OmniSharpServerCatalog.GetStartFailureMessage(exception));
    }

    [Fact]
    public void GetStartFailureMessage_IsGeneric_WhenThereIsNoException()
    {
        Assert.Equal(GenericMessage, OmniSharpServerCatalog.GetStartFailureMessage(null));
    }
}
