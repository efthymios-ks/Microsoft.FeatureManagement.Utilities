namespace Microsoft.FeatureManagement.Utilities.Samples.Web;

public static class FeatureSwitches
{
    public static class Shipping
    {
        public const string UseExpressCarrier = "Shipping.UseExpressCarrier";
        public const string UseLiveTracking = "Shipping.UseLiveTracking";
    }

    public static class Pricing
    {
        public const string V2 = "Pricing.V2";
        public const string V3 = "Pricing.V3";
    }

    public static class Recommendations
    {
        public const string Enabled = "Recommendations";
        public const string Personalized = "Recommendations.Personalized";
    }

    public static class Orders
    {
        public const string NewOrders = "Orders.NewOrders";
    }

    public static class Promotions
    {
        public const string Enabled = "Promotions";
    }
}
