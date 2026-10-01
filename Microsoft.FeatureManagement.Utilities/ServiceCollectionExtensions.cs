using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.Utilities.Internal;

namespace Microsoft.FeatureManagement.Utilities;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Puts <typeparamref name="TService"/> behind feature switches. Register the default and every
    /// implementation first: the current registration becomes the default, and each type a rule names is
    /// resolved exactly as it was registered.
    /// </summary>
    /// <remarks>
    /// The switched service is scoped when any of those registrations is scoped, transient when any is
    /// transient, and a singleton otherwise. The container disposes the default and every implementation when
    /// their scope ends; disposing the switched service itself does nothing.
    /// </remarks>
    public static IServiceCollection FeatureSwitch<TService>(
        this IServiceCollection services,
        Action<IFeatureSwitchBuilder<TService>> configure
    ) where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FeatureSwitchBuilder<TService>();
        configure(builder);

        FeatureSwitchRegistration.Apply<TService>(services, builder.Rules);

        return services;
    }
}
