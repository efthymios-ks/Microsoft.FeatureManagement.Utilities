using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricing(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IPriceCalculator, V1PriceCalculator>();
        services.TryAddSingleton<V2PriceCalculator>();
        services.TryAddSingleton(new V3PriceCalculator(discount: 0.1m));
        services.TryAddSingleton<BetaPriceCalculator>();

        return services.FeatureSwitch<IPriceCalculator>(pricing => pricing
            .When(IsBetaRequest).Then<BetaPriceCalculator>()
            .When(FeatureSwitches.Pricing.V3).Then<V3PriceCalculator>()
            .When(FeatureSwitches.Pricing.V2).Then<V2PriceCalculator>());
    }

    private static bool IsBetaRequest(IServiceProvider services)
        => string.Equals(
            services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Query["beta"].ToString(),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
}
