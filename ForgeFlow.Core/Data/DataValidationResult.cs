namespace ForgeFlow.Core.Data;

/// <summary>
/// Result of validating loaded data (items, recipes, classes, dungeons).
/// Collects errors (invalid data that was skipped) and warnings (duplicates, etc.).
/// </summary>
public sealed class DataValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>Critical issues — data was skipped or rejected.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>Non-critical issues — data was accepted but may indicate problems.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>True if no errors or warnings were recorded.</summary>
    public bool IsClean => _errors.Count == 0 && _warnings.Count == 0;

    /// <summary>True if there are no errors (warnings are acceptable).</summary>
    public bool IsValid => _errors.Count == 0;

    public void AddError(string message) => _errors.Add(message);
    public void AddWarning(string message) => _warnings.Add(message);

    public void Merge(DataValidationResult other)
    {
        _errors.AddRange(other._errors);
        _warnings.AddRange(other._warnings);
    }
}

/// <summary>
/// Validates a data definition and appends issues to the result.
/// </summary>
public static class DataValidator
{
    /// <summary>
    /// Validates that a definition has a non-empty Id. Returns false if invalid.
    /// </summary>
    public static bool ValidateId(string id, string typeName, string source, DataValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            result.AddError($"[{source}] {typeName} has empty or missing Id — skipped.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Records a duplicate-ID warning (data is still accepted — last-write-wins).
    /// </summary>
    public static void WarnDuplicate(string id, string typeName, string source, DataValidationResult result)
    {
        result.AddWarning($"[{source}] Duplicate {typeName} Id '{id}' — overwriting previous entry.");
    }
}
