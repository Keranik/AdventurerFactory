using ForgeFlow.Core.Data;

namespace ForgeFlow.Tests;

public class SoundDbTests
{
    [Fact]
    public void SoundDb_ImplementsIRegistry()
    {
        IRegistry registry = new SoundDb();
        Assert.Equal(0, registry.Count);
        registry.Clear();
    }

    [Fact]
    public void RegisterDefaults_PopulatesEntries()
    {
        var db = new SoundDb();
        db.RegisterDefaults();

        Assert.True(db.Count > 0, "RegisterDefaults should populate entries");
    }

    [Fact]
    public void Get_ReturnsRegisteredEntry()
    {
        var db = new SoundDb();
        db.Register(new SoundEntry { Id = "test_sfx", DisplayName = "Test", Category = "test" });

        var entry = db.Get("test_sfx");
        Assert.NotNull(entry);
        Assert.Equal("Test", entry!.DisplayName);
    }

    [Fact]
    public void Get_ReturnsNull_ForUnknownId()
    {
        var db = new SoundDb();
        Assert.Null(db.Get("nonexistent"));
    }

    [Fact]
    public void TryGet_ReturnsTrueForKnownEntry()
    {
        var db = new SoundDb();
        db.Register(new SoundEntry { Id = "sfx_test", Category = "ui" });

        Assert.True(db.TryGet("sfx_test", out var entry));
        Assert.NotNull(entry);
    }

    [Fact]
    public void GetByCategory_FiltersCorrectly()
    {
        var db = new SoundDb();
        db.Register(new SoundEntry { Id = "sfx_a", Category = "combat" });
        db.Register(new SoundEntry { Id = "sfx_b", Category = "ui" });
        db.Register(new SoundEntry { Id = "sfx_c", Category = "combat" });

        var combat = new List<SoundEntry>(db.GetByCategory("combat"));
        Assert.Equal(2, combat.Count);
    }

    [Fact]
    public void GetMusic_ReturnsOnlyMusicEntries()
    {
        var db = new SoundDb();
        db.Register(new SoundEntry { Id = "sfx_click", IsMusic = false });
        db.Register(new SoundEntry { Id = "music_main", IsMusic = true });

        var music = new List<SoundEntry>(db.GetMusic());
        Assert.Single(music);
        Assert.Equal("music_main", music[0].Id);
    }

    [Fact]
    public void GetSfx_ReturnsOnlyNonMusicEntries()
    {
        var db = new SoundDb();
        db.Register(new SoundEntry { Id = "sfx_click", IsMusic = false });
        db.Register(new SoundEntry { Id = "music_main", IsMusic = true });

        var sfx = new List<SoundEntry>(db.GetSfx());
        Assert.Single(sfx);
        Assert.Equal("sfx_click", sfx[0].Id);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var db = new SoundDb();
        db.RegisterDefaults();
        Assert.True(db.Count > 0);

        db.Clear();
        Assert.Equal(0, db.Count);
    }

    [Fact]
    public void DefaultEntries_HaveValidFields()
    {
        var db = new SoundDb();
        db.RegisterDefaults();

        foreach (var entry in db.GetAll())
        {
            Assert.False(string.IsNullOrEmpty(entry.Id), $"Entry has empty Id");
            Assert.False(string.IsNullOrEmpty(entry.AssetPath), $"Entry {entry.Id} has empty AssetPath");
            Assert.False(string.IsNullOrEmpty(entry.Category), $"Entry {entry.Id} has empty Category");
            Assert.InRange(entry.DefaultVolume, 0f, 1f);
        }
    }
}
