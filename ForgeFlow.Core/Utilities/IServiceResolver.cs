using System.Diagnostics.CodeAnalysis;

namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Read-only view of the composition root — resolves services by type.
/// Consumed by every caller that previously reached into <see cref="GameBootstrapper"/>
/// public properties. The resolver guarantees each registered type resolves to
/// the same singleton instance for the lifetime of the container.
/// <para>
/// This is the sanctioned replacement for the pre-Session-1 pattern of
/// "<c>bootstrapper.SomeManager</c>". New code should take an
/// <see cref="IServiceResolver"/> via constructor injection; hot-path code should
/// still resolve once at construction and cache the result in a private field
/// (reflection / dictionary lookup is not appropriate for per-tick work).
/// </para>
/// </summary>
public interface IServiceResolver
{
    /// <summary>
    /// Resolves a service of the requested type. Throws
    /// <see cref="InvalidOperationException"/> if the type is not registered.
    /// </summary>
    T Get<T>() where T : class;

    /// <summary>
    /// Non-generic resolve for reflection / auto-registration scenarios.
    /// </summary>
    object Get(Type type);

    /// <summary>
    /// Returns <see langword="true"/> and the resolved instance when the type is
    /// registered; otherwise returns <see langword="false"/> and sets
    /// <paramref name="service"/> to <see langword="null"/>.
    /// </summary>
    bool TryGet<T>([NotNullWhen(true)] out T? service) where T : class;

    /// <summary>
    /// Returns <see langword="true"/> when a registration exists for
    /// <typeparamref name="T"/>, without forcing resolution.
    /// </summary>
    bool IsRegistered<T>() where T : class;
}
