using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Appearance workshop logic. Tracks which hero/palette/template is being edited;
/// actual changes are applied on demand via AppearanceApplier.
/// </summary>
public sealed class AppearanceWorkshopLogic : ActivityEntity
{
    public ulong? SelectedHeroId { get; set; }
    public string? ActivePaletteId { get; set; }
    public string? ActiveTemplateId { get; set; }

    public AppearanceWorkshopLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 1.0f;
    }

    public override void Tick(float deltaTime)
    {
        // Appearance changes are applied on demand via the AppearanceApplier system.
    }
}
