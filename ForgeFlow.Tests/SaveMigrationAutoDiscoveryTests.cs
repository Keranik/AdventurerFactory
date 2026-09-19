using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using ForgeFlow.Core.Save;
using ForgeFlow.Core.Save.Migrations;
using Xunit;

namespace ForgeFlow.Tests;

/// <summary>
/// Safety-net coverage for <see cref="SaveMigrationRegistry"/>'s auto-discovery
/// pathway. Closes the "forgot to add the migration to the ordered list" bug
/// class: a broken chain, a duplicate edge, or a missing parameterless ctor
/// fails these tests rather than silently corrupting saves at runtime.
/// </summary>
public class SaveMigrationAutoDiscoveryTests
{
    private static readonly Assembly CoreAssembly = typeof(SaveMigrationRegistry).Assembly;
    private static readonly Type MarkerType = typeof(ISaveMigration);

    private static List<Type> ConcreteMigrationTypes()
    {
        Type[] types;
        try
        {
            types = CoreAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        return types
            .Where(t => !t.IsAbstract && !t.IsInterface && MarkerType.IsAssignableFrom(t))
            .ToList();
    }

    [Fact]
    public void EveryConcreteMigration_HasPublicParameterlessConstructor()
    {
        var concretes = ConcreteMigrationTypes();
        Assert.NotEmpty(concretes);

        foreach (var type in concretes)
        {
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            Assert.True(
                ctor != null,
                $"{type.FullName} must declare a public parameterless constructor for SaveMigrationRegistry auto-discovery.");
        }
    }

    [Fact]
    public void DiscoverMigrations_ReturnsEveryConcreteMigration()
    {
        var concretes = ConcreteMigrationTypes().OrderBy(t => t.FullName).ToList();
        var discovered = SaveMigrationRegistry.DiscoverMigrations()
            .Select(m => m.GetType())
            .OrderBy(t => t.FullName)
            .ToList();
        Assert.Equal(concretes, discovered);
    }

    [Fact]
    public void Registry_Constructs_WithContiguousChainEndingAtCurrentVersion()
    {
        var registry = new SaveMigrationRegistry();
        Assert.True(registry.Count > 0);

        // End-to-end: starting from the earliest known version produces
        // SaveData.CurrentVersion with all registered migrations applied.
        var root = new JsonObject { ["Version"] = "1.0.0" };
        var final = registry.Migrate(root);
        Assert.Equal(SaveData.CurrentVersion, final);
        Assert.Equal(SaveData.CurrentVersion, root["Version"]!.GetValue<string>());
    }
}
