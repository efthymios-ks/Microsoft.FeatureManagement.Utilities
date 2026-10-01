namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public sealed class LiveTrackingShippingProvider
{
    public Task<ShipmentTracking> TrackAsync(string trackingNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(new ShipmentTracking("Live tracking", trackingNumber, "Out for delivery, 3 stops away"));
}
