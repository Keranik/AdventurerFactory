using System.Text.Json;

namespace ForgeFlow.Core.Utilities;

public static class JsonSchemaValidator
{
    public static bool IsValidJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool HasRequiredField(JsonElement element, string fieldName)
    {
        return element.TryGetProperty(fieldName, out _);
    }

    public static bool ValidateItemEntry(JsonElement element)
    {
        return HasRequiredField(element, "id")
            && HasRequiredField(element, "tier")
            && HasRequiredField(element, "material")
            && HasRequiredField(element, "damageType");
    }

    public static bool ValidateClassEntry(JsonElement element)
    {
        return HasRequiredField(element, "id")
            && HasRequiredField(element, "name");
    }

    public static bool ValidateRecipeEntry(JsonElement element)
    {
        return HasRequiredField(element, "id")
            && HasRequiredField(element, "inputs")
            && HasRequiredField(element, "outputItemId");
    }

    public static bool ValidateDungeonEntry(JsonElement element)
    {
        return HasRequiredField(element, "id")
            && HasRequiredField(element, "tier")
            && HasRequiredField(element, "theme");
    }
}
