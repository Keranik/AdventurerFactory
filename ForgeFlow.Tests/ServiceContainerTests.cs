using ForgeFlow.Core.Utilities;

namespace ForgeFlow.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceContainer"/> — the composition root introduced
/// in Session 1 of the DI transition (Action Plan item #31).
/// Covers registration, resolution, singleton semantics, cycle detection,
/// missing-service errors, and duplicate-registration protection.
/// </summary>
public class ServiceContainerTests
{
    private sealed class Leaf { public int Value { get; set; } = 42; }

    private sealed class NeedsLeaf
    {
        public Leaf Leaf { get; }
        public NeedsLeaf(Leaf leaf) { Leaf = leaf; }
    }

    private sealed class A
    {
        public B B { get; }
        public A(B b) { B = b; }
    }

    private sealed class B
    {
        public A A { get; }
        public B(A a) { A = a; }
    }

    // ── Instance registration ────────────────────────────────────────

    [Fact]
    public void RegisterInstance_ReturnsSameInstance_OnEveryResolve()
    {
        var c = new ServiceContainer();
        var leaf = new Leaf { Value = 7 };
        c.RegisterInstance(leaf);

        Assert.Same(leaf, c.Get<Leaf>());
        Assert.Same(leaf, c.Get<Leaf>());
    }

    [Fact]
    public void RegisterInstance_NullInstance_Throws()
    {
        var c = new ServiceContainer();
        Assert.Throws<ArgumentNullException>(() => c.RegisterInstance<Leaf>(null!));
    }

    // ── Factory registration + singleton semantics ───────────────────

    [Fact]
    public void RegisterSingleton_FactoryRuns_OnFirstResolveOnly()
    {
        var c = new ServiceContainer();
        int calls = 0;
        c.RegisterSingleton<Leaf>(_ =>
        {
            calls++;
            return new Leaf();
        });

        var first = c.Get<Leaf>();
        var second = c.Get<Leaf>();
        var third = c.Get<Leaf>();

        Assert.Equal(1, calls);
        Assert.Same(first, second);
        Assert.Same(first, third);
    }

    [Fact]
    public void RegisterSingleton_ResolvesTransitiveDependencies()
    {
        var c = new ServiceContainer();
        c.RegisterSingleton<Leaf>(_ => new Leaf { Value = 99 });
        c.RegisterSingleton<NeedsLeaf>(s => new NeedsLeaf(s.Get<Leaf>()));

        var needs = c.Get<NeedsLeaf>();
        Assert.Equal(99, needs.Leaf.Value);
        Assert.Same(c.Get<Leaf>(), needs.Leaf);
    }

    // ── Error handling ───────────────────────────────────────────────

    [Fact]
    public void Get_UnregisteredType_ThrowsInvalidOperation()
    {
        var c = new ServiceContainer();
        var ex = Assert.Throws<InvalidOperationException>(() => c.Get<Leaf>());
        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void RegisterInstance_Twice_Throws()
    {
        var c = new ServiceContainer();
        c.RegisterInstance(new Leaf());
        Assert.Throws<InvalidOperationException>(() => c.RegisterInstance(new Leaf()));
    }

    [Fact]
    public void RegisterSingleton_AfterInstance_Throws()
    {
        var c = new ServiceContainer();
        c.RegisterInstance(new Leaf());
        Assert.Throws<InvalidOperationException>(() =>
            c.RegisterSingleton<Leaf>(_ => new Leaf()));
    }

    [Fact]
    public void CyclicDependency_Throws_WithPathInMessage()
    {
        var c = new ServiceContainer();
        c.RegisterSingleton<A>(s => new A(s.Get<B>()));
        c.RegisterSingleton<B>(s => new B(s.Get<A>()));

        var ex = Assert.Throws<InvalidOperationException>(() => c.Get<A>());
        Assert.Contains("Cyclic dependency", ex.Message);
        Assert.Contains("A", ex.Message);
        Assert.Contains("B", ex.Message);
    }

    [Fact]
    public void Factory_ReturningNull_Throws()
    {
        var c = new ServiceContainer();
        c.RegisterSingleton<Leaf>(_ => null!);
        Assert.Throws<InvalidOperationException>(() => c.Get<Leaf>());
    }

    // ── TryGet / IsRegistered ────────────────────────────────────────

    [Fact]
    public void TryGet_Registered_ReturnsTrue()
    {
        var c = new ServiceContainer();
        var leaf = new Leaf();
        c.RegisterInstance(leaf);

        Assert.True(c.TryGet<Leaf>(out var resolved));
        Assert.Same(leaf, resolved);
    }

    [Fact]
    public void TryGet_Unregistered_ReturnsFalseAndNull()
    {
        var c = new ServiceContainer();
        Assert.False(c.TryGet<Leaf>(out var resolved));
        Assert.Null(resolved);
    }

    [Fact]
    public void IsRegistered_DoesNotForceResolution()
    {
        var c = new ServiceContainer();
        int calls = 0;
        c.RegisterSingleton<Leaf>(_ => { calls++; return new Leaf(); });

        Assert.True(c.IsRegistered<Leaf>());
        Assert.Equal(0, calls);

        _ = c.Get<Leaf>();
        Assert.Equal(1, calls);
    }

    // ── Non-generic registration (Session 3 auto-registrar contract) ─

    [Fact]
    public void RegisterSingleton_NonGeneric_ResolvesCorrectly()
    {
        var c = new ServiceContainer();
        c.RegisterSingleton(typeof(Leaf), _ => new Leaf { Value = 5 });

        var resolved = (Leaf)c.Get(typeof(Leaf));
        Assert.Equal(5, resolved.Value);
    }
}
