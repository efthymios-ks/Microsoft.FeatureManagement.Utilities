using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void FeatureSwitch_WhenServicesIsNull_ShouldThrow()
    {
        // Act
        static void Act()
            => ((IServiceCollection)null!).FeatureSwitch<IShippingProvider>(_ => { });

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void FeatureSwitch_WhenConfigureIsNull_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }
}
