using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Utilities.Tests;

internal static class TestServices
{
    /// <summary>A provider with feature management over the given switches, validated like a development host.</summary>
    public static ServiceProvider BuildProvider(
        Action<IServiceCollection> register,
        params (string Feature, bool Enabled)[] switches
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(switches.ToDictionary(
                featureSwitch => $"FeatureManagement:{featureSwitch.Feature}",
                featureSwitch => (string?)featureSwitch.Enabled.ToString()
            ))
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddFeatureManagement();
        register(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }
}
