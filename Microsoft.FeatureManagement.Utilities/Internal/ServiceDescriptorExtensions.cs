using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

internal static class ServiceDescriptorExtensions
{
    /// <summary>The same registration, keyed: its instance, factory or type and its lifetime are kept.</summary>
    public static ServiceDescriptor WithKey(this ServiceDescriptor descriptor, object key)
        => descriptor switch
        {
            { ImplementationInstance: { } instance } => new(
                serviceType: descriptor.ServiceType,
                serviceKey: key,
                instance: instance
            ),

            { ImplementationFactory: { } factory } => new(
                serviceType: descriptor.ServiceType,
                serviceKey: key,
                factory: (provider, _) => factory(provider),
                lifetime: descriptor.Lifetime
            ),

            _ => new(
                serviceType: descriptor.ServiceType,
                serviceKey: key,
                implementationType:
                descriptor.ImplementationType!,
                lifetime: descriptor.Lifetime
            )
        };
}
