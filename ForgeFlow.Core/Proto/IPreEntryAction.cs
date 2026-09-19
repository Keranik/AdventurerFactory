using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Proto;

/// <summary>
/// Implemented by structures that need to perform a side effect on the villager
/// after eligibility passes but before the structure-specific accept step.
/// Example: DungeonPortalLogic auto-equips best weapon/armor from inventory.
/// Called by PathGateManager after <see cref="IEntryGated.CheckEntry"/> succeeds.
/// </summary>
public interface IPreEntryAction
{
    void PrepareVillagerForEntry(VillagerLogic villager);
}
