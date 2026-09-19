using ForgeFlow.Core.Entities;

namespace ForgeFlow.Core.Proto.Logic;

/// <summary>
/// Job changer — strips a worker of their current profession and combat abilities,
/// resetting them to a base citizen (level preserved).
/// Used to transition workers between combat/gathering/research roles.
/// </summary>
public sealed class JobChangerLogic : ActivityEntity
{
    public JobChangerLogic(EntityId id) : base(id)
    {
        ProcessingDuration = 5.0f;
    }

    public override void Tick(float deltaTime)
    {
        if (!IsActive) return;
    }

    /// <summary>
    /// Removes the worker's current profession and strips job-specific abilities.
    /// Returns the number of abilities lost.
    /// </summary>
    public int ChangeJob(HeroEntity worker, WorkerProfession newProfession)
    {
        int lost = worker.SwapProfession(newProfession);
        worker.RefreshAfterMaintenance();
        return lost;
    }
}
