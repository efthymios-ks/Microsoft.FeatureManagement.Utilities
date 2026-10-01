using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests.Internal;

public sealed class MethodGroupTests
{
    [Fact]
    public void Resolve_WhenTheMethodIsNotAMethodGroup_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();
        services.AddScoped<LiveTrackingShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(LiveTracking).Then<LiveTrackingShippingProvider>(
                    provider => (Func<string, CancellationToken, Task<string>>)((trackingNumber, cancellationToken)
                        => Task.FromResult(trackingNumber)),
                    liveTracking => liveTracking.TrackAsync
                ));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.Contains("is not a method group of 'IShippingProvider'", error.Message);
    }
}
