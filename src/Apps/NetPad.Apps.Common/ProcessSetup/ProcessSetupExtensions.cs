using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Apps.ProcessSetup;

public static class ProcessSetupExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TStep"/> as a singleton that is also run by <see cref="ApplyProcessSetup"/>.
    /// Steps run in the order they are registered.
    /// </summary>
    public static IServiceCollection AddProcessSetupStep<TStep>(this IServiceCollection services)
        where TStep : class, IProcessSetupStep
    {
        services.AddSingleton<TStep>();
        services.AddSingleton<IProcessSetupStep>(sp => sp.GetRequiredService<TStep>());
        return services;
    }

    /// <summary>
    /// Applies every registered <see cref="IProcessSetupStep"/> to the current process, in registration order.
    /// </summary>
    public static IServiceProvider ApplyProcessSetup(this IServiceProvider services)
    {
        foreach (var step in services.GetServices<IProcessSetupStep>())
        {
            step.ApplyToCurrentProcess();
        }

        return services;
    }
}
