using System.Reflection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>What a call is forwarded to: an instance, and the method to call on it.</summary>
internal sealed record FeatureSwitchTarget(object Instance, MethodInfo Method)
{
    public object? Invoke(object?[]? arguments)
        => Method.Invoke(Instance, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
}
