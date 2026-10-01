using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>
/// Moves the current registration of the service behind a private key as its default, checks every type
/// the rules name, and registers the proxy in its place.
/// </summary>
internal static class FeatureSwitchRegistration
{
    public static void Apply<TService>(IServiceCollection services, IReadOnlyList<FeatureSwitchRule> rules)
        where TService : class
    {
        var serviceType = typeof(TService);
        if (!serviceType.IsInterface)
        {
            throw new InvalidOperationException(
                $"'{serviceType.Name}' must be an interface to be feature-switched."
            );
        }

        var defaultRegistration = FindRegistration(services, serviceType)
            ?? throw new InvalidOperationException(
                $"'{serviceType.Name}' has no registration to fall back to. Register its default implementation before calling {nameof(ServiceCollectionExtensions.FeatureSwitch)}."
            );

        List<ServiceLifetime> lifetimes = [defaultRegistration.Lifetime];
        foreach (var implementationType in rules.Select(rule => rule.ImplementationType).Distinct())
        {
            var registration = FindRegistration(services, implementationType)
                ?? throw new InvalidOperationException(
                    $"'{implementationType.Name}' is not registered. Register it before calling {nameof(ServiceCollectionExtensions.FeatureSwitch)}<{serviceType.Name}>."
                );

            lifetimes.Add(registration.Lifetime);
        }

        var lifetime = lifetimes.Contains(ServiceLifetime.Scoped)
            ? ServiceLifetime.Scoped
            : lifetimes.Contains(ServiceLifetime.Transient)
                ? ServiceLifetime.Transient
                : ServiceLifetime.Singleton;

        var defaultKey = new FeatureSwitchKey(serviceType);
        services.Remove(defaultRegistration);
        services.Add(defaultRegistration.WithKey(defaultKey));

        services.Add(new ServiceDescriptor(
            serviceType: serviceType,
            factory: provider => FeatureSwitchProxy<TService>.Create(
                rules: rules,
                defaultKey: defaultKey,
                services: provider,
                featureManager: lifetime is ServiceLifetime.Scoped
                    ? provider.GetRequiredService<IFeatureManagerSnapshot>()
                    : provider.GetRequiredService<IFeatureManager>()
            ),
            lifetime: lifetime
        ));
    }

    private static ServiceDescriptor? FindRegistration(IServiceCollection services, Type serviceType)
        => services.LastOrDefault(descriptor
            => descriptor.ServiceType == serviceType
            && !descriptor.IsKeyedService
        );
}
