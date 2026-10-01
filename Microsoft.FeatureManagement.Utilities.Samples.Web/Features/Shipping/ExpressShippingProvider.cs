namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public sealed class ExpressShippingProvider : IShippingProvider
{
    public Task<ShippingQuote> QuoteAsync(string postcode, CancellationToken cancellationToken)
        => Task.FromResult(new ShippingQuote("Express", postcode, 9.99m, 1));

    public Task<ShipmentTracking> TrackAsync(string trackingNumber, CancellationToken cancellationToken)
        => Task.FromResult(new ShipmentTracking("Express", trackingNumber, "In transit"));
}
