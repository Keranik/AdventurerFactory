using ForgeFlow.Presentation.Unity.UI.Observers;

namespace ForgeFlow.Tests;

/// <summary>
/// Comprehensive tests for the Presentation-side observer infrastructure:
/// InspectorBinding, SubscriptionBag, and BindingBuilder (fluent API).
/// Covers change detection, force-update, disposal, edge cases, and
/// the fluent Watch().OnChanged() pattern.
/// </summary>
public class ObserverInfrastructureTests
{
    // ── InspectorBinding<T> — Basic Change Detection ──────────────

    [Fact]
    public void InspectorBinding_DoesNotFireCallback_WhenValueUnchanged()
    {
        int value = 42;
        int callbackCount = 0;
        var binding = new InspectorBinding<int>(() => value, _ => callbackCount++);

        bool changed = binding.Update();

        Assert.False(changed);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void InspectorBinding_FiresCallback_WhenValueChanges()
    {
        int value = 1;
        int receivedValue = 0;
        var binding = new InspectorBinding<int>(() => value, v => receivedValue = v);

        value = 2;
        bool changed = binding.Update();

        Assert.True(changed);
        Assert.Equal(2, receivedValue);
    }

    [Fact]
    public void InspectorBinding_SnapshotsInitialValue_OnConstruction()
    {
        int value = 99;
        var binding = new InspectorBinding<int>(() => value, _ => { });

        Assert.Equal(99, binding.CachedValue);
    }

    [Fact]
    public void InspectorBinding_TracksMultipleChanges()
    {
        int value = 0;
        var received = new List<int>();
        var binding = new InspectorBinding<int>(() => value, v => received.Add(v));

        value = 10;
        binding.Update();
        value = 20;
        binding.Update();
        value = 30;
        binding.Update();

        Assert.Equal(new[] { 10, 20, 30 }, received);
    }

    [Fact]
    public void InspectorBinding_DoesNotFireCallback_WhenValueReturnsToSame()
    {
        int value = 5;
        int callbackCount = 0;
        var binding = new InspectorBinding<int>(() => value, _ => callbackCount++);

        value = 10;
        binding.Update();
        value = 5;
        binding.Update();
        // Value is back to 5, but 5 != cached 10, so it fires
        // Now value is cached as 5, updating again should not fire
        value = 5;
        bool changed = binding.Update();

        Assert.False(changed);
        Assert.Equal(2, callbackCount); // fired for 10, then for 5
    }

    // ── InspectorBinding<T> — String Values ──────────────────────

    [Fact]
    public void InspectorBinding_WorksWithStringValues()
    {
        string value = "hello";
        string? received = null;
        var binding = new InspectorBinding<string>(() => value, v => received = v);

        value = "world";
        bool changed = binding.Update();

        Assert.True(changed);
        Assert.Equal("world", received);
    }

    [Fact]
    public void InspectorBinding_WorksWithNullableStrings()
    {
        string? value = null;
        string? received = "initial";
        var binding = new InspectorBinding<string?>(() => value, v => received = v);

        // Initial is null, no change yet
        bool changed = binding.Update();
        Assert.False(changed);

        value = "something";
        changed = binding.Update();
        Assert.True(changed);
        Assert.Equal("something", received);

        value = null;
        changed = binding.Update();
        Assert.True(changed);
        Assert.Null(received);
    }

    // ── InspectorBinding<T> — Custom Comparer ────────────────────

    [Fact]
    public void InspectorBinding_UsesCustomComparer()
    {
        // Case-insensitive comparer: "Hello" == "hello"
        string value = "Hello";
        int callbackCount = 0;
        var binding = new InspectorBinding<string>(
            () => value,
            _ => callbackCount++,
            StringComparer.OrdinalIgnoreCase);

        value = "hello"; // same ignoring case
        bool changed = binding.Update();

        Assert.False(changed);
        Assert.Equal(0, callbackCount);

        value = "WORLD"; // different
        changed = binding.Update();

        Assert.True(changed);
        Assert.Equal(1, callbackCount);
    }

    // ── InspectorBinding<T> — ForceUpdate ────────────────────────

    [Fact]
    public void InspectorBinding_ForceUpdate_FiresCallback_EvenWhenValueUnchanged()
    {
        int value = 42;
        int callbackCount = 0;
        int receivedValue = 0;
        var binding = new InspectorBinding<int>(() => value, v =>
        {
            callbackCount++;
            receivedValue = v;
        });

        binding.ForceUpdate();

        Assert.Equal(1, callbackCount);
        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void InspectorBinding_ForceUpdate_UpdatesCachedValue()
    {
        int value = 1;
        var binding = new InspectorBinding<int>(() => value, _ => { });

        value = 99;
        binding.ForceUpdate();

        Assert.Equal(99, binding.CachedValue);
    }

    // ── InspectorBinding<T> — Disposal ───────────────────────────

    [Fact]
    public void InspectorBinding_AfterDispose_UpdateReturnsFalse()
    {
        int value = 1;
        int callbackCount = 0;
        var binding = new InspectorBinding<int>(() => value, _ => callbackCount++);

        binding.Dispose();
        value = 999;
        bool changed = binding.Update();

        Assert.False(changed);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void InspectorBinding_AfterDispose_ForceUpdateDoesNothing()
    {
        int value = 1;
        int callbackCount = 0;
        var binding = new InspectorBinding<int>(() => value, _ => callbackCount++);

        binding.Dispose();
        binding.ForceUpdate();

        Assert.Equal(0, callbackCount);
    }

    // ── InspectorBinding<T> — Null Arguments ─────────────────────

    [Fact]
    public void InspectorBinding_ThrowsOnNullGetter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InspectorBinding<int>(null!, _ => { }));
    }

    [Fact]
    public void InspectorBinding_ThrowsOnNullCallback()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InspectorBinding<int>(() => 0, null!));
    }

    // ── SubscriptionBag — Bind (Direct API) ──────────────────────

    [Fact]
    public void SubscriptionBag_Bind_RegistersBinding()
    {
        var bag = new SubscriptionBag();

        bag.Bind(() => 1, _ => { });

        Assert.Equal(1, bag.Count);
    }

    [Fact]
    public void SubscriptionBag_Bind_ReturnsBinding()
    {
        var bag = new SubscriptionBag();

        var binding = bag.Bind(() => 42, _ => { });

        Assert.NotNull(binding);
        Assert.Equal(42, binding.CachedValue);
    }

    [Fact]
    public void SubscriptionBag_UpdateAll_ReturnsZero_WhenNoChanges()
    {
        int a = 1;
        int b = 2;
        var bag = new SubscriptionBag();
        bag.Bind(() => a, _ => { });
        bag.Bind(() => b, _ => { });

        int changed = bag.UpdateAll();

        Assert.Equal(0, changed);
    }

    [Fact]
    public void SubscriptionBag_UpdateAll_ReturnsCount_OfChangedBindings()
    {
        int a = 1;
        int b = 2;
        int c = 3;
        var bag = new SubscriptionBag();
        bag.Bind(() => a, _ => { });
        bag.Bind(() => b, _ => { });
        bag.Bind(() => c, _ => { });

        a = 10; // changed
        // b stays 2
        c = 30; // changed

        int changed = bag.UpdateAll();

        Assert.Equal(2, changed);
    }

    [Fact]
    public void SubscriptionBag_UpdateAll_FiresAllChangedCallbacks()
    {
        int a = 1;
        int b = 2;
        int receivedA = 0;
        int receivedB = 0;
        var bag = new SubscriptionBag();
        bag.Bind(() => a, v => receivedA = v);
        bag.Bind(() => b, v => receivedB = v);

        a = 100;
        b = 200;
        bag.UpdateAll();

        Assert.Equal(100, receivedA);
        Assert.Equal(200, receivedB);
    }

    // ── SubscriptionBag — Watch (Fluent API) ─────────────────────

    [Fact]
    public void SubscriptionBag_Watch_OnChanged_RegistersBinding()
    {
        var bag = new SubscriptionBag();

        bag.Watch(() => "hello").OnChanged(_ => { });

        Assert.Equal(1, bag.Count);
    }

    [Fact]
    public void SubscriptionBag_Watch_OnChanged_FiresOnChange()
    {
        string value = "a";
        string? received = null;
        var bag = new SubscriptionBag();
        bag.Watch(() => value).OnChanged(v => received = v);

        value = "b";
        bag.UpdateAll();

        Assert.Equal("b", received);
    }

    [Fact]
    public void SubscriptionBag_Watch_OnChanged_WithCustomComparer()
    {
        string value = "Hello";
        int callbackCount = 0;
        var bag = new SubscriptionBag();
        bag.Watch(() => value).OnChanged(_ => callbackCount++, StringComparer.OrdinalIgnoreCase);

        value = "HELLO"; // same ignoring case
        bag.UpdateAll();

        Assert.Equal(0, callbackCount);
    }

    // ── SubscriptionBag — ForceUpdateAll ─────────────────────────

    [Fact]
    public void SubscriptionBag_ForceUpdateAll_FiresAllCallbacks()
    {
        int a = 1;
        int b = 2;
        int callA = 0;
        int callB = 0;
        var bag = new SubscriptionBag();
        bag.Bind(() => a, _ => callA++);
        bag.Bind(() => b, _ => callB++);

        // No value changes — force should still fire
        bag.ForceUpdateAll();

        Assert.Equal(1, callA);
        Assert.Equal(1, callB);
    }

    // ── SubscriptionBag — Clear ──────────────────────────────────

    [Fact]
    public void SubscriptionBag_Clear_RemovesAllBindings()
    {
        var bag = new SubscriptionBag();
        bag.Bind(() => 1, _ => { });
        bag.Bind(() => 2, _ => { });

        bag.Clear();

        Assert.Equal(0, bag.Count);
    }

    [Fact]
    public void SubscriptionBag_Clear_DisposesBindings()
    {
        int value = 1;
        int callbackCount = 0;
        var bag = new SubscriptionBag();
        var binding = bag.Bind(() => value, _ => callbackCount++);

        bag.Clear();
        value = 999;
        // Binding itself is disposed, direct Update should not fire
        bool changed = binding.Update();

        Assert.False(changed);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void SubscriptionBag_Clear_AllowsNewBindings()
    {
        var bag = new SubscriptionBag();
        bag.Bind(() => 1, _ => { });
        bag.Clear();

        bag.Bind(() => 2, _ => { });

        Assert.Equal(1, bag.Count);
    }

    // ── SubscriptionBag — Dispose ────────────────────────────────

    [Fact]
    public void SubscriptionBag_Dispose_ClearsBindings()
    {
        var bag = new SubscriptionBag();
        bag.Bind(() => 1, _ => { });
        bag.Bind(() => 2, _ => { });

        bag.Dispose();

        Assert.Equal(0, bag.Count);
    }

    [Fact]
    public void SubscriptionBag_AfterDispose_UpdateAllReturnsZero()
    {
        int value = 1;
        var bag = new SubscriptionBag();
        bag.Bind(() => value, _ => { });

        bag.Dispose();
        value = 999;
        int changed = bag.UpdateAll();

        Assert.Equal(0, changed);
    }

    [Fact]
    public void SubscriptionBag_AfterDispose_BindThrows()
    {
        var bag = new SubscriptionBag();
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            bag.Bind(() => 1, _ => { }));
    }

    [Fact]
    public void SubscriptionBag_AfterDispose_WatchThrows()
    {
        var bag = new SubscriptionBag();
        bag.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            bag.Watch(() => 1));
    }

    [Fact]
    public void SubscriptionBag_DoubleDispose_DoesNotThrow()
    {
        var bag = new SubscriptionBag();
        bag.Dispose();

        var ex = Record.Exception(() => bag.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public void SubscriptionBag_AfterDispose_ForceUpdateAllDoesNothing()
    {
        int callbackCount = 0;
        var bag = new SubscriptionBag();
        bag.Bind(() => 1, _ => callbackCount++);

        bag.Dispose();
        bag.ForceUpdateAll();

        Assert.Equal(0, callbackCount);
    }

    // ── SubscriptionBag — Mixed Types ────────────────────────────

    [Fact]
    public void SubscriptionBag_SupportsHeterogeneousBindings()
    {
        int intVal = 1;
        string strVal = "a";
        bool boolVal = false;
        int intReceived = 0;
        string? strReceived = null;
        bool boolReceived = false;

        var bag = new SubscriptionBag();
        bag.Bind(() => intVal, v => intReceived = v);
        bag.Watch(() => strVal).OnChanged(v => strReceived = v);
        bag.Bind(() => boolVal, v => boolReceived = v);

        Assert.Equal(3, bag.Count);

        intVal = 42;
        strVal = "b";
        boolVal = true;
        int changed = bag.UpdateAll();

        Assert.Equal(3, changed);
        Assert.Equal(42, intReceived);
        Assert.Equal("b", strReceived);
        Assert.True(boolReceived);
    }

    // ── SubscriptionBag — Realistic Inspector Pattern ────────────

    [Fact]
    public void SubscriptionBag_RealisticUsagePattern()
    {
        // Simulate a structure logic with changing state
        var logic = new FakeStructureLogic { StoredCount = 5, StatusText = "Idle" };

        string labelText = "";
        string statusText = "";
        var bag = new SubscriptionBag();

        bag.Watch(() => logic.StoredCount)
           .OnChanged(count => labelText = $"Stored: {count}");
        bag.Watch(() => logic.StatusText)
           .OnChanged(text => statusText = text);

        // First update — no changes from initial snapshot
        int changed = bag.UpdateAll();
        Assert.Equal(0, changed);

        // Structure state changes
        logic.StoredCount = 10;
        changed = bag.UpdateAll();
        Assert.Equal(1, changed);
        Assert.Equal("Stored: 10", labelText);
        Assert.Equal("", statusText); // didn't change

        // Both change
        logic.StoredCount = 15;
        logic.StatusText = "Working";
        changed = bag.UpdateAll();
        Assert.Equal(2, changed);
        Assert.Equal("Stored: 15", labelText);
        Assert.Equal("Working", statusText);

        // Teardown
        bag.Dispose();
    }

    // ── Helper ───────────────────────────────────────────────────

    private sealed class FakeStructureLogic
    {
        public int StoredCount { get; set; }
        public string StatusText { get; set; } = "";
    }
}
