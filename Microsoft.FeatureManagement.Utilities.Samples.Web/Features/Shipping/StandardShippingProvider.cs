namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public sealed class StandardShippingProvider : IShippingProvider
{
    public Task<ShippingQuote> QuoteAsync(string postcode, CancellationToken cancellationToken)
        => Task.FromResult(new ShippingQuote("Standard", postcode, 4.99m, 5));

    public Task<ShipmentTracking> TrackAsync(string trackingNumber, CancellationToken cancellationToken)
        => Task.FromResult(new ShipmentTracking("Standard", trackingNumber, "In transit"));
}
