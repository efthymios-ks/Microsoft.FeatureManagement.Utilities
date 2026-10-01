namespace Microsoft.FeatureManagement.Utilities;

/// <summary>
/// The rules of one service, checked top to bottom on every call. The first rule that covers the method
/// being called and whose condition holds decides; when none does, the default is used.
/// </summary>
public interface IFeatureSwitchBuilder<TService>
    where TService : class
{
    /// <summary>Opens a rule that applies while the feature switch is on.</summary>
    IFeatureSwitchWhen<TService> When(string feature);

    /// <summary>
    /// Opens a rule that applies while the condition holds, such as a parameter of the current request.
    /// It is given the provider the service was resolved from, on every call.
    /// </summary>
    IFeatureSwitchWhen<TService> When(Func<IServiceProvider, bool> condition);

    /// <inheritdoc cref="When(Func{IServiceProvider, bool})"/>
    IFeatureSwitchWhen<TService> When(Func<IServiceProvider, Task<bool>> condition);
}
