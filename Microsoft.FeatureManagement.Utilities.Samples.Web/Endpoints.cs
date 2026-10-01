using Microsoft.FeatureManagement.AspNetCore;
using Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;
using Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

namespace Microsoft.FeatureManagement.Utilities.Samples.Web;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/shipping/{postcode}/quote", async (
            string postcode,
            IShippingProvider shippingProvider,
            CancellationToken cancellationToken
        ) => Results.Ok(await shippingProvider.QuoteAsync(postcode, cancellationToken)));

        app.MapGet("/shipping/{trackingNumber}/tracking", async (
            string trackingNumber,
            IShippingProvider shippingProvider,
            CancellationToken cancellationToken
        ) => Results.Ok(await shippingProvider.TrackAsync(trackingNumber, cancellationToken)));

        app.MapGet("/prices/{sku}", async (
            string sku,
            bool? beta,
            IPriceCalculator priceCalculator,
            CancellationToken cancellationToken
        ) => Results.Ok(await priceCalculator.CalculateAsync(sku, cancellationToken)));

        app.MapGet("/recommendations/{customerId}", async (
            string customerId,
            IFeatureManagerSnapshot featureManager
        ) => Results.Ok(new
        {
            customerId,
            products = await featureManager.IsEnabledAsync(FeatureSwitches.Recommendations.Personalized)
                ? new[] { "Noise-cancelling headphones", "Travel pillow" }
                : new[] { "Best seller #1", "Best seller #2" }
        }))
        .WithFeatureGate(FeatureSwitches.Recommendations.Enabled);

        app.MapGet("/orders/v1", () => Results.Ok(new[] { "ORD-1001 (legacy)" }))
            .WithFeatureGate(negate: true, FeatureSwitches.Orders.NewOrders);

        app.MapGet("/orders/v2", () => Results.Ok(new[] { "ORD-1001 (new)" }))
            .WithFeatureGate(FeatureSwitches.Orders.NewOrders);

        app.MapGet("/promotions", () => Results.Ok("10% off accessories"))
            .WithFeatureGate(FeatureSwitches.Promotions.Enabled);

        return app;
    }
}
