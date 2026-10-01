namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public sealed class V2PriceCalculator : IPriceCalculator
{
    public Task<Price> CalculateAsync(string sku, CancellationToken cancellationToken)
        => Task.FromResult(new Price("V2", sku, 19m));
}
