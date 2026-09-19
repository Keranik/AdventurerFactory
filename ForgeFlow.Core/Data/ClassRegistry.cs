using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Data.Definitions;

namespace ForgeFlow.Core.Data;

public sealed class ClassRegistry : IRegistry
{
    private readonly Dictionary<string, ClassDefinition> _classes = new();

    public void Register(ClassDefinition classDef)
    {
        _classes[classDef.Id] = classDef;
    }

    public ClassDefinition? Get(string id)
    {
        return _classes.TryGetValue(id, out var classDef) ? classDef : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out ClassDefinition? classDef)
    {
        return _classes.TryGetValue(id, out classDef);
    }

    public IEnumerable<ClassDefinition> GetAll() => _classes.Values;

    public IEnumerable<ClassDefinition> GetUnlockedAtTier(int tier)
    {
        foreach (var classDef in _classes.Values)
        {
            if (classDef.UnlockTier <= tier) yield return classDef;
        }
    }

    public IEnumerable<ClassDefinition> GetHybrids()
    {
        foreach (var classDef in _classes.Values)
        {
            if (classDef.IsHybrid) yield return classDef;
        }
    }

    public void Clear() => _classes.Clear();
    public int Count => _classes.Count;
}
