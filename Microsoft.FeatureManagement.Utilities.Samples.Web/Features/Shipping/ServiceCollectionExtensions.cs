using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShipping(this IServiceCollection services)
    {
        services.TryAddScoped<IShippingProvider, StandardShippingProvider>();
        services.TryAddScoped<ExpressShippingProvider>();
        services.TryAddScoped<LiveTrackingShippingProvider>();

        return services.FeatureSwitch<IShippingProvider>(shipping => shipping
            .When(FeatureSwitches.Shipping.UseLiveTracking).Then(
                provider => provider.TrackAsync,
                (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
            )
            .When(FeatureSwitches.Shipping.UseExpressCarrier).Then<ExpressShippingProvider>());
    }
}
