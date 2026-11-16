using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.ProcessSetup;

namespace NetPad.Apps.Common.Tests.ProcessSetup;

public class ProcessSetupExtensionsTests
{
    [Fact]
    public void Applies_Every_Registered_Step_In_Registration_Order()
    {
        var applied = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(applied);
        services.AddProcessSetupStep<SecondStep>();
        services.AddProcessSetupStep<FirstStep>();
        using var provider = services.BuildServiceProvider();

        provider.ApplyProcessSetup();

        Assert.Equal([nameof(SecondStep), nameof(FirstStep)], applied);
    }

    [Fact]
    public void A_Step_Is_The_Same_Instance_As_Its_Own_Registration()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new List<string>());
        services.AddProcessSetupStep<FirstStep>();
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<FirstStep>(), Assert.Single(provider.GetServices<IProcessSetupStep>()));
    }

    private sealed class FirstStep(List<string> applied) : IProcessSetupStep
    {
        public void ApplyToCurrentProcess() => applied.Add(nameof(FirstStep));
    }

    private sealed class SecondStep(List<string> applied) : IProcessSetupStep
    {
        public void ApplyToCurrentProcess() => applied.Add(nameof(SecondStep));
    }
}
