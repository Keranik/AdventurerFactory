namespace ForgeFlow.Core.Modding;

public interface IModExtension
{
    string ModId { get; }
    string ModName { get; }
    string Version { get; }

    void OnLoad();
    void OnUnload();
}
