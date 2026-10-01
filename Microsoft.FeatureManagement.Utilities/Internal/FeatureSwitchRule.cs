using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>
/// One <c>When</c>: its condition, the type it resolves and, for a partial, the one method it covers and the
/// method of the resolved type that replaces it.
/// </summary>
internal sealed record FeatureSwitchRule(
    FeatureSwitchCondition Condition,
    Type ImplementationType,
    MethodKey? Method,
    MethodInfo? Replacement
)
{
    private static readonly ConcurrentDictionary<(MethodInfo Replacement, MethodInfo Method), MethodInfo> _closedReplacements = new();

    public bool Covers(MethodInfo method)
        => Method is not { } covered
        || covered == MethodKey.From(method);

    public FeatureSwitchTarget CreateTarget(object instance, MethodInfo method)
        => new(instance, ReplacementFor(method));

    private MethodInfo ReplacementFor(MethodInfo method)
        => Replacement switch
        {
            null => method,
            { IsGenericMethodDefinition: true } => _closedReplacements.GetOrAdd((Replacement, method), Close),
            _ => Replacement
        };

    private static MethodInfo Close((MethodInfo Replacement, MethodInfo Method) key)
        => key.Replacement.MakeGenericMethod(key.Method.GetGenericArguments());
}
