using System.Diagnostics.CodeAnalysis;

namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Minimal dependency-injection container used as the composition root for the
/// entire game. Built in-house rather than pulling a third-party DI framework to
/// honour the Project Bible's §3.2 "avoid new libraries" rule and to keep the
/// Core assembly free of Unity / external references.
/// <para>
/// Design:
/// <list type="bullet">
/// <item><description>Every registration maps a <see cref="Type"/> to either a
/// pre-built instance or a lazy <see cref="Func{IServiceResolver, Object}"/>
/// factory.</description></item>
/// <item><description>All registered services resolve to singletons — the first
/// <see cref="Get"/> triggers the factory, the result is memoized, and every
/// later resolve returns the same instance.</description></item>
/// <item><description>Resolution is reflection-free on the hot path — factories
/// are plain delegates invoked via dictionary lookup. Cost per resolve is one
/// dictionary lookup plus, on first resolve only, the factory delegate.</description></item>
/// <item><description>Cyclic dependencies throw with the full resolution path so
/// wiring bugs surface loudly at bootstrap time instead of silently deadlocking
/// at runtime.</description></item>
/// </list>
/// </para>
/// <para>
/// The container is not thread-safe. That matches the rest of ForgeFlow —
/// construction happens on one thread at bootstrap, and after bootstrap the
/// simulation is single-threaded per §7.1.
/// </para>
/// </summary>
public sealed class ServiceContainer : IServiceResolver
{
    private readonly Dictionary<Type, object> _instances = new();
    private readonly Dictionary<Type, Func<IServiceResolver, object>> _factories = new();
    private readonly HashSet<Type> _resolutionStack = new();
    private readonly List<Type> _resolutionPath = new();

    // ── Registration API ────────────────────────────────────────────

    /// <summary>
    /// Registers an already-constructed instance under <typeparamref name="T"/>.
    /// Useful for services that are awkward to build inside a factory (e.g. the
    /// <c>IForgeLogger</c> bootstrap fallback).
    /// </summary>
    public void RegisterInstance<T>(T instance) where T : class
    {
        if (instance == null) { throw new ArgumentNullException(nameof(instance)); }
        EnsureNotYetRegistered(typeof(T));
        _instances[typeof(T)] = instance;
    }

    /// <summary>
    /// Registers a lazy singleton factory. The factory runs at most once —
    /// on the first <see cref="Get"/> for <typeparamref name="T"/> — and its
    /// result is cached for every subsequent resolve.
    /// </summary>
    public void RegisterSingleton<T>(Func<IServiceResolver, T> factory) where T : class
    {
        if (factory == null) { throw new ArgumentNullException(nameof(factory)); }
        EnsureNotYetRegistered(typeof(T));
        _factories[typeof(T)] = resolver => factory(resolver)!;
    }

    /// <summary>
    /// Registers a non-generic factory. Used by the auto-registrar in Session 3
    /// where the service type is discovered via reflection.
    /// </summary>
    public void RegisterSingleton(Type type, Func<IServiceResolver, object> factory)
    {
        if (type == null) { throw new ArgumentNullException(nameof(type)); }
        if (factory == null) { throw new ArgumentNullException(nameof(factory)); }
        EnsureNotYetRegistered(type);
        _factories[type] = factory;
    }

    // ── Resolution API ──────────────────────────────────────────────

    /// <inheritdoc />
    public T Get<T>() where T : class => (T)Get(typeof(T));

    /// <inheritdoc />
    public object Get(Type type)
    {
        if (type == null) { throw new ArgumentNullException(nameof(type)); }

        if (_instances.TryGetValue(type, out var existing))
        {
            return existing;
        }

        if (!_factories.TryGetValue(type, out var factory))
        {
            throw new InvalidOperationException(
                $"Service of type '{type.FullName}' is not registered in the container.");
        }

        if (!_resolutionStack.Add(type))
        {
            var cycle = string.Join(" → ", _resolutionPath.Select(t => t.Name)) + " → " + type.Name;
            throw new InvalidOperationException(
                $"Cyclic dependency detected while resolving '{type.Name}': {cycle}. " +
                $"Break the cycle by introducing a post-construction setter (pattern used " +
                $"by PathTrafficSystem / PathGateManager) or a lazier accessor.");
        }

        _resolutionPath.Add(type);
        try
        {
            var created = factory(this);
            if (created == null)
            {
                throw new InvalidOperationException(
                    $"Factory for service '{type.FullName}' returned null.");
            }
            _instances[type] = created;
            return created;
        }
        finally
        {
            _resolutionStack.Remove(type);
            _resolutionPath.RemoveAt(_resolutionPath.Count - 1);
        }
    }

    /// <inheritdoc />
    public bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class
    {
        if (_instances.TryGetValue(typeof(T), out var existing))
        {
            service = (T)existing;
            return true;
        }
        if (_factories.ContainsKey(typeof(T)))
        {
            service = Get<T>();
            return true;
        }
        service = null;
        return false;
    }

    /// <inheritdoc />
    public bool IsRegistered<T>() where T : class =>
        _instances.ContainsKey(typeof(T)) || _factories.ContainsKey(typeof(T));

    // ── Internals ───────────────────────────────────────────────────

    private void EnsureNotYetRegistered(Type type)
    {
        if (_instances.ContainsKey(type) || _factories.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"Service of type '{type.FullName}' is already registered. " +
                $"Duplicate registration likely indicates a wiring mistake at bootstrap.");
        }
    }
}
