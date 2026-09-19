using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Data.Definitions;

namespace ForgeFlow.Core.Data;

public sealed class DungeonRegistry : IRegistry
{
    private readonly Dictionary<string, DungeonDefinition> _dungeons = new();

    public void Register(DungeonDefinition dungeon)
    {
        _dungeons[dungeon.Id] = dungeon;
    }

    public DungeonDefinition? Get(string id)
    {
        return _dungeons.TryGetValue(id, out var dungeon) ? dungeon : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out DungeonDefinition? dungeon)
    {
        return _dungeons.TryGetValue(id, out dungeon);
    }

    public IEnumerable<DungeonDefinition> GetAll() => _dungeons.Values;

    public IEnumerable<DungeonDefinition> GetByTier(int tier)
    {
        foreach (var dungeon in _dungeons.Values)
        {
            if (dungeon.Tier == tier) yield return dungeon;
        }
    }

    public void Clear() => _dungeons.Clear();
    public int Count => _dungeons.Count;
}
