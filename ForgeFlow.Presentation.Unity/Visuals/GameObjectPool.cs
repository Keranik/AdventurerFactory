using UnityEngine;
using UObject = UnityEngine.Object;

namespace ForgeFlow.Presentation.Unity.Visuals
{

/// <summary>
/// Per-key GameObject pool. Returns deactivated GameObjects for reuse, avoiding
/// repeated Instantiate/Destroy GC pressure during rapid place/demolish cycles.
/// </summary>
internal sealed class GameObjectPool
{
    /// <summary>Maximum pooled objects per key. Older entries are dropped once the cap is hit.</summary>
    public int MaxCachedPerKey { get; set; } = 256;

    private readonly Dictionary<string, Stack<GameObject>> _stacks = new();
    private readonly Transform _poolRoot;

    public GameObjectPool(Transform poolRoot)
    {
        _poolRoot = poolRoot;
    }

    /// <summary>
    /// Returns a pooled GameObject for <paramref name="key"/>, or creates one
    /// via <paramref name="factory"/> if none is available.
    /// The object is activated and reparented to its original parent on return.
    /// </summary>
    public GameObject Get(string key, System.Func<GameObject> factory)
    {
        if (_stacks.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            var pooled = stack.Pop();
            pooled.SetActive(true);
            return pooled;
        }

        var go = factory();
        var info = go.GetComponent<PooledObjectInfo>();
        if (info == null)
        {
            info = go.AddComponent<PooledObjectInfo>();
        }
        info.PoolKey = key;
        return go;
    }

    /// <summary>
    /// Returns <paramref name="go"/> to the pool. The object is deactivated and
    /// reparented under the pool root. If the per-key cap is reached, the object
    /// is destroyed instead.
    /// </summary>
    public void Release(string key, GameObject go)
    {
        if (!_stacks.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            _stacks[key] = stack;
        }

        if (stack.Count >= MaxCachedPerKey)
        {
            UObject.Destroy(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(_poolRoot, worldPositionStays: false);
        stack.Push(go);
    }

    /// <summary>
    /// Returns <paramref name="go"/> to the pool, reading the key from its
    /// <see cref="PooledObjectInfo"/> component. No-op if the component is missing.
    /// </summary>
    public void Release(GameObject go)
    {
        var info = go.GetComponent<PooledObjectInfo>();
        if (info == null)
        {
            UObject.Destroy(go);
            return;
        }
        Release(info.PoolKey, go);
    }

    /// <summary>Destroys all pooled objects and clears the pool.</summary>
    public void Clear()
    {
        foreach (var stack in _stacks.Values)
        {
            while (stack.Count > 0)
            {
                UObject.Destroy(stack.Pop());
            }
        }
        _stacks.Clear();
    }
}

/// <summary>
/// Lightweight marker component added to every pooled GameObject so the pool
/// can look up its key when the caller calls <see cref="GameObjectPool.Release(GameObject)"/>.
/// </summary>
internal sealed class PooledObjectInfo : MonoBehaviour
{
    public string PoolKey = string.Empty;
}

}
