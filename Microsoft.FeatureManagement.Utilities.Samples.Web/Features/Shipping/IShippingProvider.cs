namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public interface IShippingProvider
{
    Task<ShippingQuote> QuoteAsync(string postcode, CancellationToken cancellationToken);

    Task<ShipmentTracking> TrackAsync(string trackingNumber, CancellationToken cancellationToken);
}
