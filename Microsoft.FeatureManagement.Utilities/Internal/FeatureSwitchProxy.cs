using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Utilities.Internal;

/// <summary>
/// Stands in for the service. Every call walks the rules for the method being called and forwards to the
/// first one whose switch is on, or to the default.
/// </summary>
internal class FeatureSwitchProxy<TService> : DispatchProxy
    where TService : class
{
    private static readonly ConcurrentDictionary<MethodInfo, Dispatcher> _dispatchers = new();

    private readonly ConcurrentDictionary<Type, object> _implementations = new();

    private IReadOnlyList<FeatureSwitchRule> _rules = [];
    private object _defaultKey = null!;
    private object? _default;
    private IServiceProvider _services = null!;
    private IFeatureManager _featureManager = null!;

    private delegate object? Dispatcher(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments);

    public static TService Create(
        IReadOnlyList<FeatureSwitchRule> rules,
        object defaultKey,
        IServiceProvider services,
        IFeatureManager featureManager
    )
    {
        var service = Create<TService, FeatureSwitchProxy<TService>>();
        var proxy = (FeatureSwitchProxy<TService>)(object)service;
        proxy._rules = rules;
        proxy._defaultKey = defaultKey;
        proxy._services = services;
        proxy._featureManager = featureManager;

        return service;
    }

    /// <summary>
    /// The container disposes every implementation it created, so disposing the proxy itself does nothing.
    /// </summary>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        if (method.DeclaringType == typeof(IDisposable))
        {
            return null;
        }

        if (method.DeclaringType == typeof(IAsyncDisposable))
        {
            return ValueTask.CompletedTask;
        }

        return _dispatchers.GetOrAdd(method, CreateDispatcher)(this, method, args);
    }

    /// <summary>
    /// One dispatcher per method, picked by its return type, so an asynchronous method can await the
    /// switches before it forwards.
    /// </summary>
    private static Dispatcher CreateDispatcher(MethodInfo method)
    {
        var returnType = method.ReturnType;
        var genericReturnType = returnType.IsGenericType ? returnType.GetGenericTypeDefinition() : null;

        var dispatcherName = returnType switch
        {
            _ when returnType == typeof(Task) => nameof(DispatchTask),
            _ when returnType == typeof(ValueTask) => nameof(DispatchValueTask),
            _ when genericReturnType == typeof(Task<>) => nameof(DispatchTaskOf),
            _ when genericReturnType == typeof(ValueTask<>) => nameof(DispatchValueTaskOf),
            _ when genericReturnType == typeof(IAsyncEnumerable<>) => nameof(DispatchAsyncEnumerableOf),
            _ => nameof(DispatchSync)
        };

        var dispatcher = typeof(FeatureSwitchProxy<TService>)
            .GetMethod(dispatcherName, BindingFlags.NonPublic | BindingFlags.Static)!;

        return (dispatcher.IsGenericMethodDefinition
            ? dispatcher.MakeGenericMethod(returnType.GetGenericArguments())
            : dispatcher
        ).CreateDelegate<Dispatcher>();
    }

    private static object? DispatchSync(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments)
        => proxy.SelectAsync(method).GetAwaiter().GetResult().Invoke(arguments);

    private static Task DispatchTask(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments)
        => proxy.InvokeTaskAsync(method, arguments);

    private static Task<TResult> DispatchTaskOf<TResult>(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments)
        => proxy.InvokeTaskAsync<TResult>(method, arguments);

    [SuppressMessage("Performance",
        "CA1859:Use concrete types when possible",
        Justification = "Bound to Dispatcher, which returns object; a ValueTask only binds once boxed."
    )]
    private static object DispatchValueTask(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments)
        => new ValueTask(proxy.InvokeValueTaskAsync(method, arguments));

    [SuppressMessage("Performance",
        "CA1859:Use concrete types when possible",
        Justification = "Bound to Dispatcher, which returns object; a ValueTask only binds once boxed."
    )]
    private static object DispatchValueTaskOf<TResult>(FeatureSwitchProxy<TService> proxy, MethodInfo method, object?[]? arguments)
        => new ValueTask<TResult>(proxy.InvokeValueTaskAsync<TResult>(method, arguments));

    private async Task InvokeTaskAsync(MethodInfo method, object?[]? arguments)
        => await (Task)(await SelectAsync(method)).Invoke(arguments)!;

    private async Task<TResult> InvokeTaskAsync<TResult>(MethodInfo method, object?[]? arguments)
        => await (Task<TResult>)(await SelectAsync(method)).Invoke(arguments)!;

    private async Task InvokeValueTaskAsync(MethodInfo method, object?[]? arguments)
        => await (ValueTask)(await SelectAsync(method)).Invoke(arguments)!;

    private async Task<TResult> InvokeValueTaskAsync<TResult>(MethodInfo method, object?[]? arguments)
        => await (ValueTask<TResult>)(await SelectAsync(method)).Invoke(arguments)!;

    private static IAsyncEnumerable<TItem> DispatchAsyncEnumerableOf<TItem>(
        FeatureSwitchProxy<TService> proxy,
        MethodInfo method,
        object?[]? arguments
    ) => proxy.InvokeAsyncEnumerable<TItem>(method, arguments);

    private async IAsyncEnumerable<TItem> InvokeAsyncEnumerable<TItem>(
        MethodInfo method,
        object?[]? arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var target = await SelectAsync(method);
        var items = (IAsyncEnumerable<TItem>)target.Invoke(arguments)!;

        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    private async Task<FeatureSwitchTarget> SelectAsync(MethodInfo method)
    {
        foreach (var rule in _rules)
        {
            if (rule.Covers(method) && await rule.Condition(_services, _featureManager))
            {
                var instance = _implementations.GetOrAdd(rule.ImplementationType, _services.GetRequiredService);

                return rule.CreateTarget(instance, method);
            }
        }

        var fallback = LazyInitializer.EnsureInitialized(
            ref _default,
            () => _services.GetRequiredKeyedService<TService>(_defaultKey)
        );

        return new FeatureSwitchTarget(fallback, method);
    }
}
