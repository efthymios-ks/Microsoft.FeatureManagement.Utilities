namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>
/// The private key the default is moved behind. Compared by reference, so switching the same service
/// twice layers the second set of rules over the first instead of replacing its default.
/// </summary>
internal sealed class FeatureSwitchKey(Type serviceType)
{
    private readonly Type _serviceType = serviceType;

    public override string ToString()
        => $"Default of '{_serviceType.Name}'";
}
