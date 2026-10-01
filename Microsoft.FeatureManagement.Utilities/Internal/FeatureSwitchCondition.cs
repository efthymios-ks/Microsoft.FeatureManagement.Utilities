namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>Whether a rule applies to the call being made.</summary>
internal delegate Task<bool> FeatureSwitchCondition(
    IServiceProvider services,
    IFeatureManager featureManager
);
