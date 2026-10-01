using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.CatalogDoubles;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests.Internal;

public sealed class FeatureSwitchWhenTests
{
    [Fact]
    public void Then_WhenGivenAType_ShouldReplaceTheImplementation()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
        }, (Express, true));
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal("express", shippingProvider.Name);
    }

    [Fact]
    public void Then_WhenTheTypeDoesNotImplementTheService_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then(typeof(LiveTrackingShippingProvider)));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith("'LiveTrackingShippingProvider' does not implement 'IShippingProvider'.", error.Message);
    }

    [Fact]
    public void Then_WhenTheTypeIsNull_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then(null!));

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void Then_WhenTheSignaturesDoNotMatch_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();
        services.AddScoped<MismatchedTrackingShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(LiveTracking).Then(
                    provider => provider.TrackAsync,
                    (MismatchedTrackingShippingProvider mismatched) => mismatched.TrackAsync
                ));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith(
            "'MismatchedTrackingShippingProvider.TrackAsync' does not match the signature of 'IShippingProvider.TrackAsync'.",
            error.Message
        );
    }

    [Fact]
    public void Then_WhenTheTypeIsTheServiceItself_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then(typeof(IShippingProvider)));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith("'IShippingProvider' cannot replace itself.", error.Message);
    }

    [Fact]
    public void Then_WhenTheGenericConstraintsDoNotMatch_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICatalog, DefaultCatalog>();
        services.AddScoped<StructCatalog>();

        // Act
        void Act()
            => services.FeatureSwitch<ICatalog>(switches => switches
                .When(UseCache).Then(
                    service => service.Describe<int>,
                    (StructCatalog structCatalog) => structCatalog.Describe<int>
                ));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith("'StructCatalog.Describe' does not match the signature of 'ICatalog.Describe'.", error.Message);
    }

    [Fact]
    public void Then_WhenOnlyTheServiceMethodIsGeneric_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICatalog, DefaultCatalog>();
        services.AddScoped<NonGenericCatalog>();

        // Act
        void Act()
            => services.FeatureSwitch<ICatalog>(switches => switches
                .When(UseCache).Then(
                    service => service.Describe<int>,
                    (NonGenericCatalog nonGeneric) => nonGeneric.Describe
                ));

        // Assert
        var error = Assert.Throws<ArgumentException>(Act);
        Assert.StartsWith("'NonGenericCatalog.Describe' does not match the signature of 'ICatalog.Describe'.", error.Message);
    }
}
