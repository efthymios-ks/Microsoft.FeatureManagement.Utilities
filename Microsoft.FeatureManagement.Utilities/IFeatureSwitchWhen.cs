using System.Linq.Expressions;

namespace Microsoft.FeatureManagement.Utilities;

/// <summary>What a rule switches to. Every type named here is resolved exactly as it was registered.</summary>
public interface IFeatureSwitchWhen<TService>
    where TService : class
{
    /// <summary>Replaces the whole implementation.</summary>
    IFeatureSwitchBuilder<TService> Then<TImplementation>()
        where TImplementation : class, TService;

    /// <summary>Replaces the whole implementation with a type known only at run time.</summary>
    IFeatureSwitchBuilder<TService> Then(Type implementationType);

    /// <summary>
    /// Replaces one method with a method of <typeparamref name="TPartial"/>, which does not have to implement
    /// the service. Both are method groups; their signatures must match, which is checked at registration.
    /// Every other method falls through to the next rule. A generic method is named with any type argument,
    /// such as <c>catalog =&gt; catalog.Describe&lt;int&gt;</c>, and the rule covers every type argument.
    /// </summary>
    IFeatureSwitchBuilder<TService> Then<TPartial>(
        Expression<Func<TService, Delegate>> method,
        Expression<Func<TPartial, Delegate>> replacement
    ) where TPartial : class;
}
