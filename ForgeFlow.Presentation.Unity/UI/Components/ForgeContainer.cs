using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// General-purpose themed layout container.
    /// Replaces raw <see cref="VisualElement"/> usage in panel/window code.
    /// Supports the full fluent builder API for consistent styling.
    /// </summary>
    internal sealed class ForgeContainer : ForgeStyledVisualElement
    {
        public ForgeContainer() { }

        public static ContainerBuilder Create()
            => new(new ForgeContainer());

        internal sealed class ContainerBuilder : ForgeBuilder<ContainerBuilder, ForgeContainer>
        {
            internal ContainerBuilder(ForgeContainer el) : base(el) { }

            public ContainerBuilder Child(VisualElement child) { _el.Add(child); return this; }
            public ContainerBuilder BackgroundColorRaw(Color c) { _el.style.backgroundColor = c; return this; }
            public ContainerBuilder PickingMode(PickingMode mode) { _el.pickingMode = mode; return this; }
            public ContainerBuilder AlignSelf(Align a) { _el.style.alignSelf = a; return this; }
            public ContainerBuilder WidthAuto() { _el.style.width = StyleKeyword.Auto; return this; }
            public ContainerBuilder HeightAuto() { _el.style.height = StyleKeyword.Auto; return this; }
            public ContainerBuilder TranslateX(float percent) { _el.style.translate = new Translate(new Length(percent, LengthUnit.Percent), 0); return this; }
        }
    }
}
