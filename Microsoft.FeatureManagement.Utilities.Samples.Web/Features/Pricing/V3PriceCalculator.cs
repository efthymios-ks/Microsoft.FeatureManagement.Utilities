namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public sealed class V3PriceCalculator(decimal discount) : IPriceCalculator
{
    public Task<Price> CalculateAsync(string sku, CancellationToken cancellationToken)
        => Task.FromResult(new Price("V3", sku, 20m * (1 - discount)));
}
