namespace ForgeFlow.Core.Utilities;

/// <summary>
/// Lightweight profiling interface for wrapping hot paths with named markers.
/// Core provides <see cref="NullProfiler"/> (zero overhead). Presentation
/// provides a Unity implementation that wraps UnityEngine.Profiling.Profiler.
/// </summary>
public interface IProfiler
{
    /// <summary>
    /// Begins a named profiling scope. Dispose the returned value to end it.
    /// Use <see cref="ProfilerScope"/> for struct-based zero-alloc disposal.
    /// </summary>
    void BeginSample(string name);

    /// <summary>Ends the most recent profiling scope.</summary>
    void EndSample();
}

/// <summary>
/// No-op profiler for headless / test usage. Zero overhead.
/// </summary>
public sealed class NullProfiler : IProfiler
{
    public static readonly NullProfiler Instance = new();

    public void BeginSample(string name) { }
    public void EndSample() { }
}

/// <summary>
/// Struct-based scope for zero-alloc profiling. Usage:
/// <code>
/// using var _ = new ProfilerScope(profiler, "MyMethod");
/// </code>
/// </summary>
public readonly struct ProfilerScope : IDisposable
{
    private readonly IProfiler _profiler;

    public ProfilerScope(IProfiler profiler, string name)
    {
        _profiler = profiler;
        _profiler.BeginSample(name);
    }

    public void Dispose()
    {
        _profiler.EndSample();
    }
}
