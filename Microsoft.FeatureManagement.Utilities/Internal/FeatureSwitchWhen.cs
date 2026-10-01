using System.Linq.Expressions;

namespace Microsoft.FeatureManagement.Utilities.Internal;

internal sealed class FeatureSwitchWhen<TService>(FeatureSwitchBuilder<TService> builder, FeatureSwitchCondition condition)
    : IFeatureSwitchWhen<TService>
    where TService : class
{
    private readonly FeatureSwitchBuilder<TService> _builder = builder;
    private readonly FeatureSwitchCondition _condition = condition;

    public IFeatureSwitchBuilder<TService> Then<TImplementation>()
        where TImplementation : class, TService
        => Then(typeof(TImplementation));

    public IFeatureSwitchBuilder<TService> Then(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        if (implementationType == typeof(TService))
        {
            var error = new ArgumentException(
                $"'{typeof(TService).Name}' cannot replace itself. Name an implementation instead.",
                nameof(implementationType)
            );

            throw error;
        }

        if (!typeof(TService).IsAssignableFrom(implementationType))
        {
            throw new ArgumentException(
                $"'{implementationType.Name}' does not implement '{typeof(TService).Name}'.",
                nameof(implementationType)
            );
        }

        return _builder.Add(new FeatureSwitchRule(_condition, implementationType, Method: null, Replacement: null));
    }

    public IFeatureSwitchBuilder<TService> Then<TPartial>(
        Expression<Func<TService, Delegate>> method,
        Expression<Func<TPartial, Delegate>> replacement
    )
        where TPartial : class
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(replacement);

        var serviceMethod = MethodGroup.Resolve(method, typeof(TService));
        var replacementMethod = MethodGroup.Resolve(replacement, typeof(TPartial));

        if (!MethodGroup.HaveSameSignature(serviceMethod, replacementMethod))
        {
            var error = new ArgumentException(
                $"'{typeof(TPartial).Name}.{replacementMethod.Name}' does not match the signature of '{typeof(TService).Name}.{serviceMethod.Name}'.",
                nameof(replacement)
            );

            throw error;
        }

        return _builder.Add(new FeatureSwitchRule(
            _condition,
            typeof(TPartial),
            MethodKey.From(serviceMethod),
            replacementMethod
        ));
    }
}
