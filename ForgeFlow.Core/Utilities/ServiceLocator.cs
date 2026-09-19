namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Minimal static service locator. Intended only as a fallback for logger and
/// cross-cutting concerns. Prefer constructor injection for all other dependencies.
/// <para>
/// The Register / Get / TryGet / IsRegistered members are marked
/// <see cref="ObsoleteAttribute"/> to fail the build-review bar for any new
/// caller. The only sanctioned production consumers are the <c>IForgeLogger</c>
/// bootstrap in <c>GameBootstrapper.RegisterLoggerAndBuses</c> and, in tests,
/// the self-tests that exercise the locator API itself. Both suppress the
/// warning locally with <c>#pragma warning disable CS0618</c>.
/// </para>
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    [Obsolete("ServiceLocator is a fallback for cross-cutting concerns only. Use constructor injection for new code.", error: false)]
    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    [Obsolete("ServiceLocator is a fallback for cross-cutting concerns only. Use constructor injection for new code.", error: false)]
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    [Obsolete("ServiceLocator is a fallback for cross-cutting concerns only. Use constructor injection for new code.", error: false)]
    public static T? TryGet<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
    }

    [Obsolete("ServiceLocator is a fallback for cross-cutting concerns only. Use constructor injection for new code.", error: false)]
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>Clears the locator. Kept un-obsoleted for legitimate test teardown.</summary>
    public static void Clear() => _services.Clear();
}
