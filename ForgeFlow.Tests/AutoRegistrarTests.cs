using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ForgeFlow.Presentation.Unity.UI;
using ForgeFlow.Presentation.Unity.UI.Inspectors;
using Xunit;

namespace ForgeFlow.Tests;

/// <summary>
/// Safety-net coverage for <see cref="AutoRegistrar"/>.
/// <para>
/// Closes the "forgot to register the new inspector" bug class
/// (Action-Plan #31 Session 3 — the <c>DungeonPortalInspectorPanel</c>
/// incident). If a new concrete inspector is added without the
/// <see cref="IAutoRegisteredInspector"/> marker, these tests fail —
/// forcing the author to wire it up before the build goes green.
/// </para>
/// <para>
/// These tests are reflection-only by design: the xUnit test host cannot
/// load <c>UnityEngine.UIElementsModule</c>, so constructing real inspector
/// instances or <see cref="UIManager"/> is not feasible here. The runtime
/// integration path runs every time the game boots via
/// <see cref="AutoRegistrar.RegisterInspectors"/>.
/// </para>
/// </summary>
public class AutoRegistrarTests
{
    private static readonly Assembly PresentationAssembly = typeof(AutoRegistrar).Assembly;
    private static readonly Type BaseInspectorType = typeof(BaseInspectorPanel);
    private static readonly Type MarkerType = typeof(IAutoRegisteredInspector);

    private static List<Type> ConcreteInspectorTypes()
    {
        Type[] types;
        try
        {
            types = PresentationAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Under the xUnit test host the Presentation assembly cannot fully
            // load every type (UnityEngine.UIElementsModule is absent).
            // Fall back to the types that did load — BaseInspectorPanel and
            // every concrete inspector load fine because UIElements is only
            // referenced transitively at method-body level.
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        return types
            .Where(t => !t.IsAbstract && !t.IsInterface && BaseInspectorType.IsAssignableFrom(t))
            .ToList();
    }

    [Fact]
    public void EveryConcreteInspector_HasAutoRegisteredMarker()
    {
        var concretes = ConcreteInspectorTypes();
        Assert.NotEmpty(concretes);

        var missing = concretes.Where(t => !MarkerType.IsAssignableFrom(t)).ToList();
        Assert.True(
            missing.Count == 0,
            $"{missing.Count} concrete inspector(s) missing IAutoRegisteredInspector marker: " +
            string.Join(", ", missing.Select(t => t.FullName)) +
            ". Add the marker so AutoRegistrar picks them up automatically.");
    }

    [Fact]
    public void DiscoverInspectorTypes_ReturnsEveryConcreteInspector()
    {
        var concretes = ConcreteInspectorTypes().OrderBy(t => t.FullName).ToList();
        var discovered = AutoRegistrar.DiscoverInspectorTypes().OrderBy(t => t.FullName).ToList();
        Assert.Equal(concretes, discovered);
    }

    [Fact]
    public void EveryAutoRegisteredInspector_HasExactlyOnePublicConstructor()
    {
        foreach (var type in AutoRegistrar.DiscoverInspectorTypes())
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.True(
                ctors.Length == 1,
                $"{type.FullName} must have exactly one public constructor for auto-registration (found {ctors.Length}).");
        }
    }
}
