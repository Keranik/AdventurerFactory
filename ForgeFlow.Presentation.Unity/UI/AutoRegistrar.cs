using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ForgeFlow.Core.Utilities;
using ForgeFlow.Presentation.Unity.UI.Inspectors;

namespace ForgeFlow.Presentation.Unity.UI
{
    /// <summary>
    /// Bootstrap-time auto-registration of Presentation-layer components.
    /// <para>
    /// Reflection is used <b>only at bootstrap</b> per Project Bible §7.1 — the
    /// hot path never touches these APIs. Each registrar method scans the
    /// current assembly for concrete types implementing a marker interface,
    /// instantiates each via constructor injection against an
    /// <see cref="IServiceResolver"/>, and registers the result with the
    /// appropriate manager.
    /// </para>
    /// <para>
    /// This closes a recurring "forgot to wire up the new class" bug class
    /// (see Action-Plan item #31 Session 3 — the <c>DungeonPortalInspectorPanel</c>
    /// incident). New inspector panels are discovered automatically once they
    /// carry <see cref="IAutoRegisteredInspector"/>, and a paired safety-net
    /// test fails the build if any concrete inspector is missing the marker.
    /// </para>
    /// </summary>
    internal static class AutoRegistrar
    {
        /// <summary>
        /// Discovers every non-abstract <see cref="IAutoRegisteredInspector"/>
        /// in the Presentation assembly, resolves each constructor parameter
        /// through <paramref name="services"/> (or from <paramref name="uiManager"/>
        /// when the parameter type is assignable from it), and registers the
        /// resulting instance with <paramref name="uiManager"/> under its own
        /// <see cref="IUIWindow.WindowId"/> at <see cref="UILayer.Gameplay"/>
        /// with <c>saveLayout: true</c>.
        /// </summary>
        /// <returns>The set of instantiated inspectors, keyed by
        /// <see cref="IUIWindow.WindowId"/>, so callers can perform
        /// post-registration wiring (e.g. hooking panel events).</returns>
        public static IReadOnlyDictionary<string, IAutoRegisteredInspector> RegisterInspectors(
            IServiceResolver services,
            UIManager uiManager)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (uiManager == null)
            {
                throw new ArgumentNullException(nameof(uiManager));
            }

            var registered = new Dictionary<string, IAutoRegisteredInspector>(StringComparer.Ordinal);
            foreach (var type in DiscoverInspectorTypes())
            {
                var instance = InstantiateInspector(type, services, uiManager);
                var descriptor = new WindowDescriptor(instance.WindowId, UILayer.Gameplay, saveLayout: true);
                uiManager.Register(descriptor, instance);
                registered[instance.WindowId] = instance;
            }
            return registered;
        }

        /// <summary>
        /// Returns every non-abstract concrete type in the Presentation
        /// assembly that implements <see cref="IAutoRegisteredInspector"/>.
        /// Exposed for the safety-net test.
        /// </summary>
        internal static IEnumerable<Type> DiscoverInspectorTypes()
        {
            var assembly = typeof(AutoRegistrar).Assembly;
            var marker = typeof(IAutoRegisteredInspector);
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Tolerate partial type-load failures (e.g. under the xUnit test
                // host where UnityEngine.UIElementsModule isn't available, or
                // when a mod assembly ships with missing optional deps).
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }
                if (!marker.IsAssignableFrom(type))
                {
                    continue;
                }
                yield return type;
            }
        }

        private static IAutoRegisteredInspector InstantiateInspector(
            Type type,
            IServiceResolver services,
            UIManager uiManager)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{type.FullName}: auto-registered inspectors must declare exactly one public constructor (found {ctors.Length}).");
            }

            var ctor = ctors[0];
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveArgument(type, parameters[i], services, uiManager);
            }

            try
            {
                return (IAutoRegisteredInspector)ctor.Invoke(args);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"{type.FullName}: constructor threw during auto-registration.", tie.InnerException);
            }
        }

        private static object ResolveArgument(
            Type ownerType,
            ParameterInfo parameter,
            IServiceResolver services,
            UIManager uiManager)
        {
            var pt = parameter.ParameterType;

            // Special case: anything the UIManager itself can satisfy
            // (UIManager, ITransientElementTracker, IUIFocusProvider, etc.).
            if (pt.IsInstanceOfType(uiManager))
            {
                return uiManager;
            }

            // Fall through to the DI container for all Core services.
            try
            {
                return services.Get(pt);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"{ownerType.FullName}: cannot auto-resolve constructor parameter '{parameter.Name}' of type {pt.FullName}. " +
                    "Register the type in GameBootstrapper.RegisterAllServices, or satisfy it via UIManager.", ex);
            }
        }
    }
}
