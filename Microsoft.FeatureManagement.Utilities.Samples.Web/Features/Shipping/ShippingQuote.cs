namespace Microsoft.FeatureManagement.Utilities.Samples.Web.Features.Shipping;

public sealed record ShippingQuote(string Carrier, string Postcode, decimal Price, int Days);
