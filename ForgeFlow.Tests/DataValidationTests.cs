using ForgeFlow.Core.Data;
using ForgeFlow.Core.Data.Definitions;
using ForgeFlow.Core.Proto.Prototypes;

namespace ForgeFlow.Tests;

public class DataValidationTests
{
    [Fact]
    public void DataValidator_EmptyId_ReportsError()
    {
        var result = new DataValidationResult();

        bool valid = DataValidator.ValidateId("", "Item", "test.json", result);

        Assert.False(valid);
        Assert.Single(result.Errors);
        Assert.Contains("empty", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataValidator_ValidId_ReturnsTrue()
    {
        var result = new DataValidationResult();

        bool valid = DataValidator.ValidateId("wood", "Item", "test.json", result);

        Assert.True(valid);
        Assert.True(result.IsClean);
    }

    [Fact]
    public void DataValidator_DuplicateWarning_RecordsWarning()
    {
        var result = new DataValidationResult();

        DataValidator.WarnDuplicate("wood", "Item", "test.json", result);

        Assert.Single(result.Warnings);
        Assert.True(result.IsValid); // warnings don't make it invalid
        Assert.False(result.IsClean); // but it's not clean
    }

    [Fact]
    public void MergeItems_CorruptJson_DoesNotCrash()
    {
        var itemRegistry = new ItemRegistry();
        var classRegistry = new ClassRegistry();
        var recipeRegistry = new RecipeRegistry();
        var dungeonRegistry = new DungeonRegistry();
        var loader = new DataLoader(itemRegistry, classRegistry, recipeRegistry, dungeonRegistry);

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{ this is not valid JSON !!!");

            var result = loader.MergeItems(tempFile);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains("Invalid JSON", result.Errors[0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MergeItems_DuplicateId_RecordsWarning()
    {
        var itemRegistry = new ItemRegistry();
        var classRegistry = new ClassRegistry();
        var recipeRegistry = new RecipeRegistry();
        var dungeonRegistry = new DungeonRegistry();
        var loader = new DataLoader(itemRegistry, classRegistry, recipeRegistry, dungeonRegistry);

        // Pre-register an item
        itemRegistry.Register(new ItemProto { Id = "wood", DisplayName = "Wood" });

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "[{\"Id\":\"wood\",\"Category\":\"Resource\"}]");

            var result = loader.MergeItems(tempFile);

            Assert.True(result.IsValid); // accepted (last-write-wins)
            Assert.Single(result.Warnings);
            Assert.Contains("Duplicate", result.Warnings[0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DataValidationResult_Merge_CombinesResults()
    {
        var a = new DataValidationResult();
        a.AddError("err1");
        a.AddWarning("warn1");

        var b = new DataValidationResult();
        b.AddError("err2");

        a.Merge(b);

        Assert.Equal(2, a.Errors.Count);
        Assert.Single(a.Warnings);
    }
}
