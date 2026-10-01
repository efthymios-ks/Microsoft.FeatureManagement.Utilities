namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public sealed class BetaPriceCalculator : IPriceCalculator
{
    public Task<Price> CalculateAsync(string sku, CancellationToken cancellationToken)
        => Task.FromResult(new Price("Beta", sku, 15m));
}
