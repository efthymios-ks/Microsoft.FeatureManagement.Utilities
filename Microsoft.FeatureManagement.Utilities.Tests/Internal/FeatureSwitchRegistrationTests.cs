using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests.Internal;

public sealed class FeatureSwitchRegistrationTests
{
    [Fact]
    public void Apply_WhenTheServiceIsNotAnInterface_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<StandardShippingProvider>(_ => { });

        // Assert
        var error = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("'StandardShippingProvider' must be an interface to be feature-switched.", error.Message);
    }

    [Fact]
    public void Apply_WhenTheServiceHasNoRegistration_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(_ => { });

        // Assert
        var error = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "'IShippingProvider' has no registration to fall back to. Register its default implementation before calling FeatureSwitch.",
            error.Message
        );
    }

    [Fact]
    public void Apply_WhenARuleNamesAnUnregisteredType_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());

        // Assert
        var error = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "'ExpressShippingProvider' is not registered. Register it before calling FeatureSwitch<IShippingProvider>.",
            error.Message
        );
    }

    [Fact]
    public void Apply_WhenTheDefaultIsAFactory_ShouldKeepTheFactory()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider>(_ => new ExpressShippingProvider());
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal("express", shippingProvider.Name);
    }

    [Fact]
    public void Apply_WhenTheDefaultIsAnInstance_ShouldKeepTheInstance()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddSingleton<IShippingProvider>(new OvernightShippingProvider());
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });

        // Act
        var shippingProvider = provider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal("overnight", shippingProvider.Name);
    }

    [Fact]
    public void Apply_WhenEveryRegistrationIsASingleton_ShouldRegisterASingleton()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddSingleton<IShippingProvider, StandardShippingProvider>();
            services.AddSingleton<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
        }, (Express, true));

        // Act
        var first = provider.GetRequiredService<IShippingProvider>();
        var second = provider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Same(first, second);
        Assert.Equal("express", first.Name);
    }

    [Fact]
    public void Apply_WhenAnyRegistrationIsScoped_ShouldRegisterAScopedService()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddSingleton<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
        });

        // Act
        void Act()
            => provider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Theory]
    [InlineData(false, false, "standard")]
    [InlineData(true, false, "express")]
    [InlineData(false, true, "overnight")]
    [InlineData(true, true, "overnight")]
    public void Apply_WhenCalledTwiceForTheSameService_ShouldCheckTheLaterRulesFirst(
        bool express,
        bool overnight,
        string expected
    )
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.AddScoped<OvernightShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Overnight).Then<OvernightShippingProvider>());
        }, (Express, express), (Overnight, overnight));
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal(expected, shippingProvider.Name);
    }

    [Fact]
    public void Apply_WhenThePartialIsNotRegistered_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(LiveTracking).Then(
                    provider => provider.TrackAsync,
                    (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
                ));

        // Assert
        var error = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "'LiveTrackingShippingProvider' is not registered. Register it before calling FeatureSwitch<IShippingProvider>.",
            error.Message
        );
    }

    [Fact]
    public void Apply_WhenTheDefaultIsTransient_ShouldRegisterATransientService()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });
        using var scope = provider.CreateScope();

        // Act
        var first = scope.ServiceProvider.GetRequiredService<IShippingProvider>();
        var second = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Apply_WhenRegistrationsAreTransientAndScoped_ShouldRegisterAScopedService()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
        });
        using var scope = provider.CreateScope();

        // Act
        var first = scope.ServiceProvider.GetRequiredService<IShippingProvider>();
        var second = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Apply_WhenRegistrationsAreTransientAndSingletons_ShouldRegisterATransientService()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.AddSingleton<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
        });
        using var scope = provider.CreateScope();

        // Act
        var first = scope.ServiceProvider.GetRequiredService<IShippingProvider>();
        var second = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Apply_WhenASingletonDependsOnAScopedImplementation_ShouldFailTheBuild()
    {
        // Arrange
        static void Register(IServiceCollection services)
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>());
            services.AddSingleton<ShippingDesk>();
        }

        // Act
        static void Act()
            => TestServices.BuildProvider(Register).Dispose();

        // Assert
        var error = Assert.Throws<AggregateException>(Act);
        Assert.Contains("Cannot consume scoped service", error.Message);
    }

    [Fact]
    public void Apply_WhenTheDefaultIsScoped_ShouldRegisterAScopedService()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });
        using var scope = provider.CreateScope();

        // Act
        var first = scope.ServiceProvider.GetRequiredService<IShippingProvider>();
        var second = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Apply_WhenTheServiceIsTransient_ShouldBeResolvableFromTheRoot()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });

        // Act
        var shippingProvider = provider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal("standard", shippingProvider.Name);
    }
}
