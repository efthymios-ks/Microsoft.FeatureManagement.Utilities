using Microsoft.Extensions.DependencyInjection;
using static Microsoft.FeatureManagement.Utilities.Tests.CatalogDoubles;
using static Microsoft.FeatureManagement.Utilities.Tests.PrinterDoubles;
using static Microsoft.FeatureManagement.Utilities.Tests.ShippingDoubles;

namespace Microsoft.FeatureManagement.Utilities.Tests.Internal;

public sealed class FeatureSwitchProxyTests
{
    [Fact]
    public async Task Invoke_WhenNoSwitchIsOn_ShouldUseTheDefault()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(RegisterCarriers);
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);

        // Assert
        Assert.Equal("standard:10557", quote);
    }

    [Theory]
    [InlineData(false, "standard")]
    [InlineData(true, "express")]
    public async Task Invoke_WhenTheSwitchIsOn_ShouldForwardEveryKindOfMember(bool express, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(RegisterCarriers, (Express, express));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var name = shippingProvider.Name;
        var description = shippingProvider.Describe("10557");
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var booking = await shippingProvider.BookAsync("10557");
        await shippingProvider.PingAsync();

        // Assert
        Assert.Equal(expected, name);
        Assert.Equal($"{expected}:10557", description);
        Assert.Equal($"{expected}:10557", quote);
        Assert.Equal($"{expected}:10557", booking);
    }

    [Theory]
    [InlineData(false, false, "standard")]
    [InlineData(false, true, "express")]
    [InlineData(true, false, "overnight")]
    [InlineData(true, true, "overnight")]
    public void Invoke_WhenSeveralSwitchesAreOn_ShouldUseTheFirstRule(bool overnight, bool express, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(RegisterCarriers, (Overnight, overnight), (Express, express));
        using var scope = provider.CreateScope();

        // Act
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Assert
        Assert.Equal(expected, shippingProvider.Name);
    }

    [Theory]
    [InlineData(false, "standard")]
    [InlineData(true, "express")]
    public async Task Invoke_WhenTheImplementationThrows_ShouldSurfaceTheOriginalException(bool express, string expected)
    {
        // Arrange
        using var provider = TestServices.BuildProvider(RegisterCarriers, (Express, express));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        Task Act()
            => shippingProvider.CancelAsync("TRK");

        // Assert
        var error = await Assert.ThrowsAsync<InvalidOperationException>(Act);
        Assert.Equal(expected, error.Message);
    }

    [Theory]
    [InlineData(false, false, "standard:10557", "standard:TRK")]
    [InlineData(false, true, "standard:10557", "live:TRK")]
    [InlineData(true, false, "express:10557", "express:TRK")]
    [InlineData(true, true, "express:10557", "live:TRK")]
    public async Task Invoke_WhenPartialAndWholeRulesAreMixed_ShouldRouteEachMethodToTheFirstRuleCoveringIt(
        bool express,
        bool liveTracking,
        string expectedQuote,
        string expectedTracking
    )
    {
        // Arrange
        using var provider = TestServices.BuildProvider(RegisterLiveTracking, (Express, express), (LiveTracking, liveTracking));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var tracking = await shippingProvider.TrackAsync("TRK", CancellationToken.None);

        // Assert
        Assert.Equal(expectedQuote, quote);
        Assert.Equal(expectedTracking, tracking);
    }

    [Fact]
    public async Task Invoke_WhenThePartialComesAfterIt_ShouldBeShadowedByTheWholeReplacement()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.AddScoped<LiveTrackingShippingProvider>();

            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<ExpressShippingProvider>()
                .When(LiveTracking).Then(
                    provider => provider.TrackAsync,
                    (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
                ));
        }, (Express, true), (LiveTracking, true));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var tracking = await shippingProvider.TrackAsync("TRK", CancellationToken.None);

        // Assert
        Assert.Equal("express:TRK", tracking);
    }

    [Fact]
    public async Task Invoke_WhenThePartialIsAFullImplementation_ShouldTakeOnlyTheNamedMethod()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();

            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then(
                    provider => provider.QuoteAsync,
                    (ExpressShippingProvider expressProvider) => expressProvider.QuoteAsync
                ));
        }, (Express, true));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var quote = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var tracking = await shippingProvider.TrackAsync("TRK", CancellationToken.None);

        // Assert
        Assert.Equal("express:10557", quote);
        Assert.Equal("standard:TRK", tracking);
        Assert.Equal("standard", shippingProvider.Name);
    }

    [Fact]
    public async Task Invoke_WhenOneResolutionIsCalledRepeatedly_ShouldKeepOneDefaultInstance()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, CountingShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var first = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var second = await shippingProvider.QuoteAsync("10557", CancellationToken.None);

        // Assert
        Assert.Equal("counting:1", first);
        Assert.Equal("counting:2", second);
    }

    [Fact]
    public async Task Invoke_WhenOneResolutionIsCalledRepeatedly_ShouldKeepOneImplementationInstance()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, StandardShippingProvider>();
            services.AddTransient<CountingShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(Express).Then<CountingShippingProvider>());
        }, (Express, true));
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var first = await shippingProvider.QuoteAsync("10557", CancellationToken.None);
        var second = await shippingProvider.QuoteAsync("10557", CancellationToken.None);

        // Assert
        Assert.Equal("counting:1", first);
        Assert.Equal("counting:2", second);
    }

    [Fact]
    public async Task Invoke_WhenTheServiceIsResolvedAgain_ShouldStartFromAFreshInstance()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddTransient<IShippingProvider, CountingShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(_ => { });
        });
        using var scope = provider.CreateScope();
        var firstResolution = scope.ServiceProvider.GetRequiredService<IShippingProvider>();
        var secondResolution = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var first = await firstResolution.QuoteAsync("10557", CancellationToken.None);
        var second = await secondResolution.QuoteAsync("10557", CancellationToken.None);

        // Assert
        Assert.Equal("counting:1", first);
        Assert.Equal("counting:1", second);
    }

    [Fact]
    public void Invoke_WhenTheScopeIsDisposed_ShouldNotThrow()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IReceiptPrinter, ReceiptPrinter>();
            services.FeatureSwitch<IReceiptPrinter>(_ => { });
        });
        var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IReceiptPrinter>();

        // Act
        void Act()
            => scope.Dispose();

        // Assert
        Assert.Null(Record.Exception(Act));
    }

    [Fact]
    public void Invoke_WhenTheScopeIsDisposed_ShouldLeaveDisposingToTheContainer()
    {
        // Arrange
        var printer = new ReceiptPrinter();
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IReceiptPrinter>(_ => printer);
            services.FeatureSwitch<IReceiptPrinter>(_ => { });
        });
        var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IReceiptPrinter>().Print();

        // Act
        scope.Dispose();

        // Assert
        Assert.Equal(1, printer.Disposals);
    }

    [Fact]
    public async Task Invoke_WhenTheScopeIsDisposedAsynchronously_ShouldLeaveDisposingToTheContainer()
    {
        // Arrange
        var printer = new ReceiptPrinter();
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IReceiptPrinter>(_ => printer);
            services.FeatureSwitch<IReceiptPrinter>(_ => { });
        });
        var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IReceiptPrinter>().Print();

        // Act
        await scope.DisposeAsync();

        // Assert
        Assert.Equal(1, printer.Disposals);
    }

    [Fact]
    public void Invoke_WhenTheMethodIsGeneric_ShouldBuildTheReplacementForTheCalledTypeArguments()
    {
        // Arrange
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<ICatalog, DefaultCatalog>();
            services.AddScoped<CachedCatalog>();
            services.FeatureSwitch<ICatalog>(switches => switches
                .When(UseCache).Then(
                    service => service.Describe<int>,
                    (CachedCatalog cached) => cached.Describe<int>
                ));
        }, (UseCache, true));
        using var scope = provider.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalog>();

        // Act
        var described = catalog.Describe<string>();

        // Assert
        Assert.Equal("cached:String", described);
    }

    [Fact]
    public async Task Invoke_WhenTheMethodReturnsAnAsyncStream_ShouldNotBlock()
    {
        // Arrange
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var provider = TestServices.BuildProvider(services =>
        {
            services.AddScoped<IShippingProvider, StandardShippingProvider>();
            services.AddScoped<ExpressShippingProvider>();
            services.FeatureSwitch<IShippingProvider>(shipping => shipping
                .When(_ => gate.Task).Then<ExpressShippingProvider>());
        });
        using var scope = provider.CreateScope();
        var shippingProvider = scope.ServiceProvider.GetRequiredService<IShippingProvider>();

        // Act
        var call = Task.Run(() => shippingProvider.StreamAsync("10557"));
        var returnedBeforeTheSwitch = await Task.WhenAny(call, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)) == call;
        gate.SetResult(true);
        List<string> items = [];
        await foreach (var item in await call)
        {
            items.Add(item);
        }

        // Assert
        Assert.True(returnedBeforeTheSwitch);
        Assert.Equal(["express:10557"], items);
    }

    private static void RegisterCarriers(IServiceCollection services)
    {
        services.AddScoped<IShippingProvider, StandardShippingProvider>();
        services.AddScoped<ExpressShippingProvider>();
        services.AddScoped<OvernightShippingProvider>();

        services.FeatureSwitch<IShippingProvider>(shipping => shipping
            .When(Overnight).Then<OvernightShippingProvider>()
            .When(Express).Then<ExpressShippingProvider>());
    }

    private static void RegisterLiveTracking(IServiceCollection services)
    {
        services.AddScoped<IShippingProvider, StandardShippingProvider>();
        services.AddScoped<ExpressShippingProvider>();
        services.AddScoped<LiveTrackingShippingProvider>();

        services.FeatureSwitch<IShippingProvider>(shipping => shipping
            .When(LiveTracking).Then(
                provider => provider.TrackAsync,
                (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
            )
            .When(Express).Then<ExpressShippingProvider>());
    }
}
