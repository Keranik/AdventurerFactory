using System.Diagnostics.CodeAnalysis;

namespace ForgeFlow.Core.Commands;

/// <summary>
/// Maps command type names to their CLR types for deserialization during replay.
/// Explicit registration keeps replay deterministic and avoids reflection scanning.
/// </summary>
public sealed class CommandLogRegistry
{
    private readonly Dictionary<string, Type> _typesByName = new();

    /// <summary>Registers a command type for replay deserialization.</summary>
    public void Register<T>() where T : IGameCommand
    {
        _typesByName[typeof(T).Name] = typeof(T);
    }

    /// <summary>Resolves a command type name to its CLR type.</summary>
    public bool TryResolve(string typeName, [NotNullWhen(true)] out Type? type)
    {
        return _typesByName.TryGetValue(typeName, out type);
    }

    /// <summary>
    /// Registers all built-in command types. Call during bootstrap.
    /// </summary>
    public void RegisterDefaults()
    {
        Register<PlaceStructureCommand>();
        Register<DrawPathCommand>();
        Register<PlacePathGateCommand>();
        Register<DemolishCommand>();
        Register<RotateEntityCommand>();
        Register<SetRecipeCommand>();
        Register<SetFilterCommand>();
        Register<SetStockpileFilterCommand>();
    }
}
