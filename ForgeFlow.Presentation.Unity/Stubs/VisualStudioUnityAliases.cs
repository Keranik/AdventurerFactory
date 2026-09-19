// ──────────────────────────────────────────────────────────────
// Previously this file contained global using aliases that mapped
// stubs from ForgeFlow.Presentation.Unity.Stubs.* into the global
// scope.  Those aliases caused CS0433 duplicate-type errors because
// the compiled DLL defined types that shadowed real Unity types.
//
// All stub types now live in their correct Unity namespaces
// (UnityEngine, UnityEngine.UI, UnityEngine.UIElements, etc.)
// inside VisualStudioUnityStubs.cs, so normal 'using UnityEngine;'
// directives resolve to the stubs during VS builds and to the real
// Unity types inside the Unity Editor.  No aliases are needed.
// ──────────────────────────────────────────────────────────────
