using System.Text.Json.Nodes;

namespace ForgeFlow.Core.Save.Migrations;

/// <summary>
/// Represents a single save-data migration step that transforms raw JSON
/// from one schema version to the next. Migrations run before deserialization
/// into <see cref="SaveData"/>, so they operate on the untyped JSON tree.
/// </summary>
public interface ISaveMigration
{
    /// <summary>The version this migration upgrades from.</summary>
    string FromVersion { get; }

    /// <summary>The version this migration upgrades to.</summary>
    string ToVersion { get; }

    /// <summary>
    /// Applies the migration in-place on <paramref name="root"/>.
    /// Must update the "Version" property to <see cref="ToVersion"/>.
    /// </summary>
    void Apply(JsonObject root);
}
