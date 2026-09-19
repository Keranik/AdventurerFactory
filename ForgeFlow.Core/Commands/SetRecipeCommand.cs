namespace ForgeFlow.Core.Commands;

/// <summary>
/// Command to set or clear the active recipe on a craft station.
/// Pass null <see cref="RecipeId"/> to clear the recipe.
/// Handled by <see cref="Systems.StructureManager"/>.
/// </summary>
public readonly struct SetRecipeCommand : IGameCommand
{
    public ulong StationId { get; }
    public string? RecipeId { get; }

    public SetRecipeCommand(ulong stationId, string? recipeId)
    {
        StationId = stationId;
        RecipeId = recipeId;
    }

    public override string ToString() =>
        RecipeId != null
            ? $"SetRecipe station {StationId} to {RecipeId}"
            : $"ClearRecipe station {StationId}";
}
