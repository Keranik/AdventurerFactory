using System.Diagnostics.CodeAnalysis;
using ForgeFlow.Core.Data.Definitions;

namespace ForgeFlow.Core.Data;

public sealed class RecipeRegistry : IRegistry
{
    private readonly Dictionary<string, RecipeDefinition> _recipes = new();

    public void Register(RecipeDefinition recipe)
    {
        _recipes[recipe.Id] = recipe;
    }

    public RecipeDefinition? Get(string id)
    {
        return _recipes.TryGetValue(id, out var recipe) ? recipe : null;
    }

    public bool TryGet(string id, [NotNullWhen(true)] out RecipeDefinition? recipe)
    {
        return _recipes.TryGetValue(id, out recipe);
    }

    public IEnumerable<RecipeDefinition> GetAll() => _recipes.Values;

    public IEnumerable<RecipeDefinition> GetByOutputItem(string outputItemId)
    {
        foreach (var recipe in _recipes.Values)
        {
            if (recipe.OutputItemId == outputItemId) yield return recipe;
        }
    }

    public IEnumerable<RecipeDefinition> GetByTier(int tier)
    {
        foreach (var recipe in _recipes.Values)
        {
            if (recipe.RequiredTier <= tier) yield return recipe;
        }
    }

    public void Clear() => _recipes.Clear();
    public int Count => _recipes.Count;
}
