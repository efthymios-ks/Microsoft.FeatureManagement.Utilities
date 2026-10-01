namespace Microsoft.FeatureManagement.Utilities.Internal;

internal sealed class FeatureSwitchBuilder<TService> : IFeatureSwitchBuilder<TService>
    where TService : class
{
    private readonly List<FeatureSwitchRule> _rules = [];

    public IReadOnlyList<FeatureSwitchRule> Rules
        => _rules;

    public IFeatureSwitchWhen<TService> When(string feature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);

        return new FeatureSwitchWhen<TService>(
            builder: this,
            condition: (_, featureManager) => featureManager.IsEnabledAsync(feature)
        );
    }

    public IFeatureSwitchWhen<TService> When(Func<IServiceProvider, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new FeatureSwitchWhen<TService>(
            builder: this,
            condition: (services, _) => Task.FromResult(condition(services))
        );
    }

    public IFeatureSwitchWhen<TService> When(Func<IServiceProvider, Task<bool>> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new FeatureSwitchWhen<TService>(
            builder: this,
            condition: (services, _) => condition(services)
        );
    }

    public IFeatureSwitchBuilder<TService> Add(FeatureSwitchRule rule)
    {
        _rules.Add(rule);

        return this;
    }
}
