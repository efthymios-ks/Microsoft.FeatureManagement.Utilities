using System.Diagnostics.CodeAnalysis;

namespace Microsoft.FeatureManagement.Utilities.Tests;

public static class ShippingDoubles
{
    public const string Express = "Shipping.UseExpressCarrier";
    public const string Overnight = "Shipping.UseOvernightCarrier";
    public const string LiveTracking = "Shipping.UseLiveTracking";

    public interface IShippingProvider
    {
        string Name { get; }

        string Describe(string postcode);

        Task<string> QuoteAsync(string postcode, CancellationToken cancellationToken);

        Task<string> TrackAsync(string trackingNumber, CancellationToken cancellationToken);

        ValueTask<string> BookAsync(string postcode);

        Task CancelAsync(string trackingNumber);

        ValueTask PingAsync();

        IAsyncEnumerable<string> StreamAsync(string postcode);
    }

    public class RecordingShippingProvider(string name) : IShippingProvider
    {
        public string Name
            => name;

        public string Describe(string postcode)
            => $"{name}:{postcode}";

        public Task<string> QuoteAsync(string postcode, CancellationToken cancellationToken)
            => Task.FromResult($"{name}:{postcode}");

        public Task<string> TrackAsync(string trackingNumber, CancellationToken cancellationToken)
            => Task.FromResult($"{name}:{trackingNumber}");

        public ValueTask<string> BookAsync(string postcode)
            => ValueTask.FromResult($"{name}:{postcode}");

        public Task CancelAsync(string trackingNumber)
            => throw new InvalidOperationException(name);

        public ValueTask PingAsync()
            => ValueTask.CompletedTask;

        public async IAsyncEnumerable<string> StreamAsync(string postcode)
        {
            await Task.Yield();

            yield return $"{name}:{postcode}";
        }
    }

    public sealed class StandardShippingProvider() : RecordingShippingProvider("standard");

    public sealed class ExpressShippingProvider() : RecordingShippingProvider("express");

    public sealed class OvernightShippingProvider() : RecordingShippingProvider("overnight");

    public sealed class LiveTrackingShippingProvider
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Mirrors IShippingProvider.TrackAsync.")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A replacement is bound as an instance method group.")]
        public Task<string> TrackAsync(string trackingNumber, CancellationToken cancellationToken = default)
            => Task.FromResult($"live:{trackingNumber}");
    }

    public sealed class CountingShippingProvider() : RecordingShippingProvider("counting"), IShippingProvider
    {
        private int _quotes;

        public new Task<string> QuoteAsync(string postcode, CancellationToken cancellationToken)
            => Task.FromResult($"counting:{++_quotes}");
    }

    public sealed class RequestFlags
    {
        public bool Beta { get; set; }
    }

    public sealed class ShippingDesk(IShippingProvider shippingProvider)
    {
        public string Carrier
            => shippingProvider.Name;
    }

    public sealed class MismatchedTrackingShippingProvider
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A replacement is bound as an instance method group.")]
        public Task<string> TrackAsync(string trackingNumber)
            => Task.FromResult($"mismatched:{trackingNumber}");
    }
}
