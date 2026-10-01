namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public sealed class V1PriceCalculator : IPriceCalculator
{
    public Task<Price> CalculateAsync(string sku, CancellationToken cancellationToken)
        => Task.FromResult(new Price("V1", sku, 20m));
}
