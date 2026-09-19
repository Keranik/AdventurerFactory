namespace ForgeFlow.Presentation.Unity.UI.Inspectors
{
    /// <summary>
    /// Marker interface for inspector panels that should be discovered and
    /// registered automatically by <see cref="AutoRegistrar.RegisterInspectors"/>.
    /// <para>
    /// Every concrete inspector panel deriving from <see cref="BaseInspectorPanel"/>
    /// must implement this interface. A safety-net test
    /// (<c>AutoRegistrarTests.EveryConcreteInspector_IsAutoRegistered</c>) fails
    /// the build if any concrete inspector is missing the marker — closing the
    /// "forgot to register the new inspector" bug class that previously hid
    /// <see cref="DungeonPortalInspectorPanel"/>.
    /// </para>
    /// <para>
    /// Requirements for auto-registration:
    ///   - Exactly one public constructor.
    ///   - Every constructor parameter type is either registered in the
    ///     <see cref="ForgeFlow.Core.Utilities.IServiceResolver"/> container, or
    ///     is assignable from the <see cref="UIManager"/> instance (e.g.
    ///     <see cref="UIManager"/> itself or <see cref="ITransientElementTracker"/>).
    ///   - <c>WindowId</c> must return a unique <see cref="WindowIds"/> constant.
    /// </para>
    /// <para>
    /// Descriptor defaults to <c>UILayer.Gameplay</c> with
    /// <c>saveLayout: true</c> — the layout every current inspector uses. Future
    /// inspectors needing a different layer/layout can override the marker with
    /// a richer interface if the need arises.
    /// </para>
    /// </summary>
    internal interface IAutoRegisteredInspector : IUIWindow
    {
    }
}
