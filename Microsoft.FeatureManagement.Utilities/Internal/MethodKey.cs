using System.Reflection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>Identifies an interface method whichever way its <see cref="MethodInfo"/> was obtained.</summary>
internal readonly record struct MethodKey(Type? DeclaringType, int MetadataToken)
{
    public static MethodKey From(MethodInfo method)
        => new(method.DeclaringType, method.MetadataToken);
}
