# Microsoft.FeatureManagement.Utilities

Feature switches for services, on top of `Microsoft.FeatureManagement`.
```
ServiceCollectionExtensions.cs            FeatureSwitch<TService>
IFeatureSwitchBuilder.cs                  When(feature), When(services => …)
IFeatureSwitchWhen.cs                     Then<T>(), Then(type), Then(method, replacement)
Internal/FeatureSwitchRegistration.cs     moves the default behind a key, checks every rule
Internal/FeatureSwitchProxy.cs            stands in for the service, picks the target per call
Internal/MethodGroup.cs                   reads the method out of provider => provider.TrackAsync
```

Switches are read from the `FeatureManagement` section, so any source feature management reads.  
`appsettings.json`, Azure App Configuration, environment variables turns them on and off.

```csharp
builder.Services.AddFeatureManagement();
```

## Switching an implementation

```csharp
public sealed class StandardShippingProvider : IShippingProvider;
public sealed class ExpressShippingProvider : IShippingProvider;
public sealed class OvernightShippingProvider : IShippingProvider;
```

```csharp
services.TryAddScoped<IShippingProvider, StandardShippingProvider>();
services.TryAddScoped<ExpressShippingProvider>(); // Implementation, not interface
services.TryAddScoped<OvernightShippingProvider>(); // Implementation, not interface

services.FeatureSwitch<IShippingProvider>(shipping => shipping
    .When("Shipping.UseOvernightCarrier").Then<OvernightShippingProvider>()
    .When("Shipping.UseExpressCarrier").Then(typeof(ExpressShippingProvider)));
```

```json
{
  "FeatureManagement": {
    "Shipping.UseOvernightCarrier": false,
    "Shipping.UseExpressCarrier": true
  }
}
```

Register everything first, then switch it.  
The registration `IShippingProvider` already has becomes the default,  
and every type a rule names is resolved exactly as it was registered.  
Its lifetime, its factory, an `AddHttpClient` or a package's own `Add…` all stay yours.  
That also means a service you do not own can be switched: whatever its package registered is the default.

| Call | Does |
| --- | --- |
| `FeatureSwitch<TService>(…)` | puts the service behind its rules; the current registration becomes the default |
| `When(feature)` | opens a rule that applies while the switch is on |
| `When(services => …)` / `When(async services => …)` | opens a rule that applies while the condition holds, see below |
| `Then<TImplementation>()` / `Then(type)` | replaces the whole implementation |
| `Then(method, replacement)` | replaces one method, see below |

## Conditions

```csharp
services.AddHttpContextAccessor();

services.FeatureSwitch<IPriceCalculator>(pricing => pricing
    .When(services => services
        .GetRequiredService<IHttpContextAccessor>()
        .HttpContext?.Request.Query.ContainsKey("beta") == true)
    .Then<BetaPriceCalculator>()
    .When("Pricing.V2").Then<V2PriceCalculator>());
```

A condition is given the provider the service was resolved from.  
That is the request scope for a scoped service, or the root for a singleton.  
It runs on every call,  
so it can read the current request, a scoped service, or anything else registered.  
It takes its place in the order like any other rule.

## Replacing one method

```csharp
public sealed class LiveTrackingShippingProvider
{
    public Task<ShipmentTracking> TrackAsync(
        string trackingNumber, 
        CancellationToken cancellationToken
    ) { … }
}
```

```csharp
services.TryAddScoped<IShippingProvider, StandardShippingProvider>();
services.TryAddScoped<ExpressShippingProvider>(); // Implementation, not interface
services.TryAddScoped<LiveTrackingShippingProvider>(); // Implementation, not interface

services.FeatureSwitch<IShippingProvider>(shipping => shipping
    .When("Shipping.UseLiveTracking").Then(
        provider => provider.TrackAsync,
        (LiveTrackingShippingProvider liveTracking) => liveTracking.TrackAsync
    )
    .When("Shipping.UseExpressCarrier").Then<ExpressShippingProvider>());
```

The replacement only needs the one method.  
It does not implement the interface.  
Both lambdas are method groups,  
and their signatures are compared when the rule is registered.  
A replacement whose signature drifts from the interface stops the host at start-up.  
The replacement can also come from a full implementation, to take only one of its methods:  
`(ExpressShippingProvider express) => express.QuoteAsync`.

A generic method is replaced for every type argument.  
`catalog => catalog.Describe<int>` names `Describe<TItem>` itself,  
so a call to `Describe<string>` goes to the replacement's `Describe<string>`.  
The replacement needs as many type parameters,  
and no constraint the interface method does not have.

An overloaded method is named through a cast to its delegate type:  
`provider => (Func<string, CancellationToken, Task<ShipmentTracking>>)provider.TrackAsync`.

## Order

Rules are checked top to bottom, **per method, on every call**.  
The first rule that covers the method, and whose switch is on or condition holds, decides.  
A rule for one method is skipped for every other method.  
When no rule decides, the default is used.

| Switches on | QuoteAsync | TrackAsync |
| --- | --- | --- |
| (none) | Standard | Standard |
| UseExpressCarrier | Express | Express |
| UseLiveTracking | Standard | Live tracking |
| UseExpressCarrier + UseLiveTracking | Express | Live tracking |

Put the most specific rule first.  
A whole replacement written above a one-method rule covers that method too,  
so the one-method rule is never reached.

## Lifetime

The switched service takes its lifetime from the default and every type a rule names.  
It is scoped when any of them is scoped,  
transient when any is transient,  
and a singleton otherwise.  
So a singleton that depends on it still fails at start-up whenever one of them is scoped.  
Each instance of the switched service resolves the default and every implementation once, on first use,  
and keeps them for every later call.

A scoped service reads switches through the per-request snapshot,  
so one request sees one set of switches however often it calls the service.  
A transient or singleton service reads them on every call.  
A changed switch applies with no restart.

Disposing is left to the container.  
It disposes the default and every implementation it created,  
so disposing the switched service itself does nothing.  
A service you dispose yourself is released when its scope ends,  
or when the host stops for one resolved from the root.

## Synchronous members

Conditions are asynchronous, and so is reading a switch.  
A member that returns a task awaits them before it forwards.  
A member that returns an `IAsyncEnumerable<T>` returns at once,  
and reads them when the stream is first enumerated.  
Any other member, a property included,  
waits for them on the calling thread.

## Mistakes caught at start-up

`FeatureSwitch` checks everything it can while it registers,  
so a mistake stops the host rather than the first request:

```
'IShippingProvider' has no registration to fall back to. Register its default implementation before calling FeatureSwitch.

'ExpressShippingProvider' is not registered. Register it before calling FeatureSwitch<IShippingProvider>.

'LiveTrackingShippingProvider' does not implement 'IShippingProvider'.

'IShippingProvider' cannot replace itself. Name an implementation instead.

'provider => …' is not a method group of 'IShippingProvider', such as 'provider => provider.TrackAsync'.

'LiveTrackingShippingProvider.TrackAsync' does not match the signature of 'IShippingProvider.TrackAsync'.

'ShippingService' must be an interface to be feature-switched.
```

## Endpoints

Endpoints need nothing from this library.  
`Microsoft.FeatureManagement.AspNetCore` already gates them:

```csharp
app.MapGet("/orders/v1", () => …).WithFeatureGate(negate: true, "Orders.NewOrders");

app.MapGet("/orders/v2", () => …).WithFeatureGate("Orders.NewOrders");
```

## Testing

A switch is plain configuration, so a test sets it like any other value:

```csharp
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureManagement:Shipping.UseExpressCarrier"] = "true" })
    .Build();

services.AddSingleton<IConfiguration>(configuration);
services.AddFeatureManagement();
services.AddShipping();
```

The implementations themselves are ordinary classes, tested on their own with no switch involved.

## Sample

`Microsoft.FeatureManagement.Utilities.Samples.Web` uses every feature above.  
Run it and Swagger UI opens at `/swagger`, with every endpoint ready to call,  
and flipping a value in its `appsettings.json` applies on the next request.

## License

MIT.
