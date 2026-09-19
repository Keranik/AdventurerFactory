using System.Text.Json.Nodes;

namespace ForgeFlow.Core.Save.Migrations;

/// <summary>
/// Migrates save data from version 1.0.0 to 1.2.0.
/// Ensures collections added in 1.2.0 exist in the JSON tree.
/// </summary>
public sealed class Migration_1_0_0_To_1_2_0 : ISaveMigration
{
    public string FromVersion => "1.0.0";
    public string ToVersion => "1.2.0";

    public void Apply(JsonObject root)
    {
        root["PathGates"] ??= new JsonArray();
        root["Villagers"] ??= new JsonArray();
        root["HotbarSlots"] ??= new JsonArray();
        root["CompletedTutorialIds"] ??= new JsonArray();
        root["AchievementProgress"] ??= new JsonArray();
        root["Version"] = ToVersion;
    }
}
