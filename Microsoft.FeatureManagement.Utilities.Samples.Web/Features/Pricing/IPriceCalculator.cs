namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Pricing;

public interface IPriceCalculator
{
    Task<Price> CalculateAsync(string sku, CancellationToken cancellationToken);
}
