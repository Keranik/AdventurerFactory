using ForgeFlow.Core.Data;

namespace ForgeFlow.Tests;

public class AssetManifestTests
{
    [Fact]
    public void AssetManifest_ImplementsIRegistry()
    {
        IRegistry registry = new AssetManifest();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void RegisterDefaults_PopulatesEntries()
    {
        var manifest = new AssetManifest();
        manifest.RegisterDefaults();

        Assert.True(manifest.Count > 0);
    }

    [Fact]
    public void Get_ReturnsRegisteredEntry()
    {
        var manifest = new AssetManifest();
        manifest.Register(new AssetEntry { Id = "test_asset", DisplayName = "Test", Type = AssetType.Sprite, Path = "test/path" });

        var entry = manifest.Get("test_asset");
        Assert.NotNull(entry);
        Assert.Equal("Test", entry!.DisplayName);
        Assert.Equal(AssetType.Sprite, entry.Type);
    }

    [Fact]
    public void Get_ReturnsNull_ForUnknownId()
    {
        var manifest = new AssetManifest();
        Assert.Null(manifest.Get("nonexistent"));
    }

    [Fact]
    public void GetByType_FiltersCorrectly()
    {
        var manifest = new AssetManifest();
        manifest.Register(new AssetEntry { Id = "sprite_a", Type = AssetType.Sprite });
        manifest.Register(new AssetEntry { Id = "prefab_a", Type = AssetType.Prefab });
        manifest.Register(new AssetEntry { Id = "sprite_b", Type = AssetType.Sprite });

        var sprites = new List<AssetEntry>(manifest.GetByType(AssetType.Sprite));
        Assert.Equal(2, sprites.Count);
    }

    [Fact]
    public void GetByTag_FiltersCorrectly()
    {
        var manifest = new AssetManifest();
        manifest.Register(new AssetEntry { Id = "a", Tags = { "structure", "tier1" } });
        manifest.Register(new AssetEntry { Id = "b", Tags = { "character" } });
        manifest.Register(new AssetEntry { Id = "c", Tags = { "structure", "tier2" } });

        var structures = new List<AssetEntry>(manifest.GetByTag("structure"));
        Assert.Equal(2, structures.Count);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var manifest = new AssetManifest();
        manifest.RegisterDefaults();
        Assert.True(manifest.Count > 0);

        manifest.Clear();
        Assert.Equal(0, manifest.Count);
    }

    [Fact]
    public void DefaultEntries_HaveValidFields()
    {
        var manifest = new AssetManifest();
        manifest.RegisterDefaults();

        foreach (var entry in manifest.GetAll())
        {
            Assert.False(string.IsNullOrEmpty(entry.Id), $"Entry has empty Id");
            Assert.False(string.IsNullOrEmpty(entry.Path), $"Entry {entry.Id} has empty Path");
        }
    }
}
