using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests.Internal;

public sealed class FeatureSwitchBuilderTests
{
    [Theory]
    [InlineData("?beta=true", "express")]
    [InlineData("?other=true", "standard")]
    [InlineData("", "standard")]
    public void When_WhenTheConditionReadsIt_ShouldFollowTheRequest(string query, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(IsBetaRequest).Then<ExpressShippingProvider>());
        });
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            Request = { QueryString = new QueryString(query) }
        };
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal(expected, shippingProvider.Name);
    }

    [Theory]
    [InlineData(true, "express")]
    [InlineData(false, "standard")]
    public void When_WhenTheConditionUsesAScopedService_ShouldGetTheScopeTheServiceCameFrom(bool beta, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<RequestFlags>();
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(current => current.GetRequiredService<RequestFlags>().Beta)
                .Then<ExpressShippingProvider>()
            );
        });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<RequestFlags>().Beta = beta;

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal(expected, shippingProvider.Name);
    }

    [Theory]
    [InlineData(true, "express")]
    [InlineData(false, "standard")]
    public async Task When_WhenItIsAsynchronous_ShouldAwaitTheCondition(bool beta, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(async _ =>
                {
                    await Task.Yield();

                    return beta;
                })
                .Then<ExpressShippingProvider>());
        });
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);

        // Assert
        Assert.Equal($"{expected}:10557", quote);
    }

    [Theory]
    [InlineData(false, false, "standard")]
    [InlineData(false, true, "express")]
    [InlineData(true, false, "overnight")]
    [InlineData(true, true, "overnight")]
    public void When_WhenConditionsAndSwitchesAreMixed_ShouldKeepTheOrderTheyWereWritten(
        bool beta,
        bool express,
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
                .When(_ => beta).Then<OvernightShippingProvider>()
                .When(Express).Then<ExpressShippingProvider>()
            );
        }, (Express, express));
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal(expected, shippingProvider.Name);
    }

    [Fact]
    public async Task When_WhenTheConditionCoversOneMethod_ShouldLeaveTheOthersAlone()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<LiveTrackingShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(_ => true).Then(
                    provider => provider.TrackAsync,
                    (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
                ));
        });
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var tracking = await shippingProvider.TrackAsync("TRK", CancellationToken.None);

        // Assert
        Assert.Equal("standard:10557", quote);
        Assert.Equal("live:TRK", tracking);
    }

    [Fact]
    public void When_WhenTheConditionIsNull_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When((Func<IServiceProvider, bool>)null!));

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void When_WhenTheAsynchronousConditionIsNull_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When((Func<IServiceProvider, Task<bool>>)null!));

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void When_WhenTheFeatureIsBlank_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping.When(" "));

        // Assert
        Assert.Throws<ArgumentException>(Act);
    }

    [Fact]
    public void When_WhenTheFeatureIsNull_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IShippingProvider, StandardShippingProvider>();

        // Act
        void Act()
            => services.FeatureSwitch<IShippingProvider>(shipping => shipping.When((string)null!));

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    private static bool IsBetaRequest(IServiceProvider services)
        => services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Query.ContainsKey("beta") == true;
}
