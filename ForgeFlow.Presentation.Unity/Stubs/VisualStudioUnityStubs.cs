// =============================================================================
// Unity API Stubs for Visual Studio / headless builds.
// Derived from: Unity 2022.3 LTS (2022.3.20f1)
// Last verified: April 2026
//
// These stubs exist solely to enable compilation outside the Unity Editor.
// They must match the real Unity API surface used by ForgeFlow.Presentation.Unity.
// See Action-Plan.md Change #15 for the long-term plan to replace these with
// Unity's official reference assemblies.
// =============================================================================
#if UNITY_STUBS
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    internal class Object
    {
        public string name { get; set; } = string.Empty;
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
    }

    internal class Component : Object
    {
        public GameObject gameObject { get; internal set; } = null!;
        public Transform transform { get; internal set; } = null!;
        public T? GetComponent<T>() where T : class => default;
    }

    internal class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    internal class MonoBehaviour : Behaviour { }

    internal struct Mathf
    {
        public static float Round(float f) => (float)Math.Round(f, MidpointRounding.AwayFromZero);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float Abs(float f) => Math.Abs(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp01(float value) => Clamp(value, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Infinity = float.PositiveInfinity;
        public const float Epsilon = 1.401298E-45f;
    }

    internal class GameObject : Object
    {
        public Transform transform { get; } = null!;
        public string tag { get; set; } = "Untagged";
        public bool activeSelf { get; private set; } = true;
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public void SetActive(bool active) => activeSelf = active;
        public T AddComponent<T>() where T : Component, new() => new();
        public T? GetComponent<T>() where T : class => default;
        public static GameObject CreatePrimitive(PrimitiveType type) => new(type.ToString());
    }

    internal class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Quaternion rotation { get; set; } = Quaternion.identity;
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Transform? parent { get; set; }
        public void SetParent(Transform? parent, bool worldPositionStays = true) => this.parent = parent;
    }

    internal struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 forward => new(0, 0, 1);
        public static Vector3 right => new(1, 0, 0);
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    internal struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new(0, 0);
    }

    internal struct Vector2Int
    {
        public int x;
        public int y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    internal struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public static Quaternion identity => new() { w = 1 };
        public static Quaternion Euler(float x, float y, float z) => identity;
    }

    internal struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color white => new(1, 1, 1);
        public static Color red => new(1, 0, 0);
        public static Color green => new(0, 1, 0);
        public static Color yellow => new(1, 1, 0);
        public static Color clear => new(0, 0, 0, 0);
    }

    internal static class Debug
    {
        public static void Log(object message) => Console.WriteLine($"[Unity] {message}");
        public static void LogWarning(object message) => Console.WriteLine($"[Unity WARN] {message}");
        public static void LogError(object message) => Console.Error.WriteLine($"[Unity ERROR] {message}");
    }

    internal static class Time
    {
        public static float deltaTime { get; set; } = 1f / 60f;
        public static float time { get; set; }
        public static float fixedDeltaTime { get; set; } = 1f / 60f;
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class SerializeFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    internal class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new();
    }

    internal static class Resources
    {
        public static T? Load<T>(string path) where T : Object => default;
    }

    internal class Font : Object
    {
        public static Font? CreateDynamicFontFromOSFont(string fontName, int size) => new();
    }

    internal enum KeyCode
    {
        None, Space, Return, Escape, Tab, Backspace, Delete,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        UpArrow, DownArrow, LeftArrow, RightArrow,
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,
        Mouse0, Mouse1, Mouse2,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }

    internal static class Input
    {
        public static Vector3 mousePosition => Vector3.zero;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyUp(KeyCode key) => false;
    }

    internal struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 GetPoint(float distance) => origin + direction * distance;
    }

    internal struct RaycastHit
    {
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Collider? collider;
        public GameObject? transform;
    }

    internal class Collider : Component { }

    internal static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance = float.MaxValue)
        {
            hit = default;
            return false;
        }
    }

    internal struct Plane
    {
        private Vector3 _normal;
        private float _distance;

        public Plane(Vector3 normal, Vector3 point)
        {
            _normal = normal;
            _distance = -(normal.x * point.x + normal.y * point.y + normal.z * point.z);
        }

        public bool Raycast(Ray ray, out float enter)
        {
            float denom = _normal.x * ray.direction.x + _normal.y * ray.direction.y + _normal.z * ray.direction.z;
            if (Mathf.Abs(denom) < 1e-6f)
            {
                enter = 0f;
                return false;
            }

            float numer = -(_normal.x * ray.origin.x + _normal.y * ray.origin.y + _normal.z * ray.origin.z + _distance);
            enter = numer / denom;
            return enter > 0f;
        }
    }

    internal enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    internal class Camera : Behaviour
    {
        public static Camera? main { get; set; }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; } = 5f;
        public float nearClipPlane { get; set; } = 0.3f;
        public float farClipPlane { get; set; } = 1000f;
        public Color backgroundColor { get; set; } = Color.clear;
        public CameraClearFlags clearFlags { get; set; }
        public Ray ScreenPointToRay(Vector3 screenPos) => new(Vector3.zero, Vector3.forward);
    }

    internal enum LightType { Spot, Directional, Point, Area }
    internal enum LightShadows { None, Hard, Soft }

    internal class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; } = Color.white;
        public float intensity { get; set; } = 1f;
        public LightShadows shadows { get; set; }
    }

    internal enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    internal enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }

    internal enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    internal class Renderer : Component
    {
        public Material? material { get; set; }
        public Material[]? materials { get; set; }
    }

    internal class MeshRenderer : Renderer { }

    internal class MeshFilter : Component
    {
        public Mesh? mesh { get; set; }
        public Mesh? sharedMesh { get; set; }
    }

    internal class Mesh : Object { }

    internal class Material : Object
    {
        public Color color { get; set; } = Color.white;
        public int renderQueue { get; set; }
        public Material() { }
        public Material(Shader shader) { }
        public Material(Material source) { color = source.color; }
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetInt(string name, int value) { }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public bool HasProperty(string name) => false;
    }

    internal class Shader : Object
    {
        public static Shader? Find(string name) => null;
    }

    internal class GUIStyle
    {
        public int fontSize { get; set; } = 14;
        public Color normal_textColor { get; set; } = Color.white;
    }

    internal static class GUI
    {
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
    }

    internal struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    internal static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
    }

    internal class AudioSource : Component
    {
        public float volume { get; set; } = 1f;
        public bool loop { get; set; }
        public AudioClip? clip { get; set; }
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip? clip, float volumeScale = 1f) { }
    }

    internal class AudioClip : Object
    {
        public float length { get; set; }
    }

    internal static class Application
    {
        public static string dataPath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public static string streamingAssetsPath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StreamingAssets");
        public static string persistentDataPath => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeFlow");
    }

    internal class ParticleSystem : Component
    {
        public MainModule main => new();
        public EmissionModule emission => new();
        public void Play() { }
        public void Stop() { }
        public bool isPlaying => false;

        public struct MainModule
        {
            public float startLifetime { get; set; }
            public float startSpeed { get; set; }
            public float startSize { get; set; }
            public Color startColor { get; set; }
            public int maxParticles { get; set; }
            public bool loop { get; set; }
            public float duration { get; set; }
        }

        public struct EmissionModule
        {
            public float rateOverTime { get; set; }
        }
    }

    internal enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    internal class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
    }

    internal class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; } = new(100, 30);
        public Vector2 pivot { get; set; } = new(0.5f, 0.5f);
    }

    internal class LayoutElement : Behaviour
    {
        public float preferredHeight { get; set; }
        public float preferredWidth { get; set; }
    }

    internal class VerticalLayoutGroup : Behaviour
    {
        public int spacing { get; set; }
        public RectOffset padding { get; set; } = new();
    }

    internal class HorizontalLayoutGroup : Behaviour
    {
        public int spacing { get; set; }
    }

    internal class RectOffset
    {
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
}

namespace UnityEngine.UI
{
    internal class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; } = new(1920, 1080);
    }

    internal class GraphicRaycaster : Behaviour { }

    internal class Button : Behaviour
    {
        public ButtonClickedEvent onClick { get; } = new();

        public class ButtonClickedEvent
        {
            public void AddListener(Action call) { }
            public void RemoveListener(Action call) { }
        }
    }

    internal class Image : Behaviour
    {
        public Color color { get; set; } = Color.white;
    }

    internal class Text : Behaviour
    {
        public string text { get; set; } = string.Empty;
        public int fontSize { get; set; } = 14;
        public Color color { get; set; } = Color.white;
    }
}

namespace UnityEngine.EventSystems
{
    internal class EventSystem : Behaviour { }
    internal class StandaloneInputModule : Behaviour { }
}

namespace UnityEngine.InputSystem.UI
{
    internal class InputSystemUIInputModule : UnityEngine.EventSystems.Behaviour { }
}

namespace TMPro
{
    using UnityEngine;

    internal class TextMeshProUGUI : Behaviour
    {
        public string text { get; set; } = string.Empty;
        public float fontSize { get; set; } = 14f;
        public Color color { get; set; } = Color.white;
        public TextAlignmentOptions alignment { get; set; }
    }

    internal class TMP_Text : Behaviour
    {
        public string text { get; set; } = string.Empty;
        public float fontSize { get; set; } = 14f;
        public TextAlignmentOptions alignment { get; set; }
        public Color color { get; set; } = Color.white;
    }

    internal enum TextAlignmentOptions
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }
}

namespace UnityEngine.InputSystem
{

    internal enum InputActionType { Value, Button, PassThrough }

    internal class InputAction
    {
        public string name { get; } = string.Empty;
        public event Action<CallbackContext>? started;
        public event Action<CallbackContext>? performed;
        public event Action<CallbackContext>? canceled;

        public InputAction() { }

        public InputAction(string? name = null, InputActionType type = default, string? binding = null, string? interactions = null, string? processors = null, string? expectedControlLayout = null)
        {
            this.name = name ?? string.Empty;
        }

        public void Enable() { }
        public void Disable() { }
        public TValue ReadValue<TValue>() where TValue : struct => default;
        public void AddBinding(string path) { }
        public void InvokeStarted() => started?.Invoke(default);
        public void InvokePerformed() => performed?.Invoke(default);
        public void InvokeCanceled() => canceled?.Invoke(default);

        public struct CallbackContext
        {
            public TValue ReadValue<TValue>() where TValue : struct => default;
            public bool ReadValueAsButton() => false;
        }
    }

    internal class InputActionMap
    {
        public string name { get; set; } = string.Empty;
        public void Enable() { }
        public void Disable() { }
        public InputAction AddAction(string name, InputActionType type = default) => new(name: name, type: type);
    }

    internal class InputActionAsset : ScriptableObject
    {
        private readonly Dictionary<string, InputAction> _actions = new();

        public InputActionMap AddActionMap(string name) => new BoundInputActionMap(this, name);
        public InputActionMap? FindActionMap(string name, bool throwIfNotFound = false) => null;

        public InputAction? FindAction(string name, bool throwIfNotFound = false)
        {
            _actions.TryGetValue(name, out var action);
            return action;
        }

        public void Enable() { }
        public void Disable() { }
        internal void RegisterAction(string name, InputAction action) => _actions[name] = action;

        private sealed class BoundInputActionMap : InputActionMap
        {
            private readonly InputActionAsset _owner;

            public BoundInputActionMap(InputActionAsset owner, string name)
            {
                _owner = owner;
                this.name = name;
            }

            public new InputAction AddAction(string name, InputActionType type = default)
            {
                var action = new InputAction(name: name, type: type);
                _owner.RegisterAction(name, action);
                return action;
            }
        }
    }

    internal class PlayerInput : Behaviour { }
}

namespace UnityEngine.UIElements
{
    using System.Collections;

    internal class VisualElement
    {
        public string name { get; set; } = string.Empty;
        public IStyle style => new StyleStub();
        public VisualElement? parent { get; set; }
        public void Add(VisualElement child) { }
        public void Remove(VisualElement child) { }
        public void Clear() { }
        public void AddToClassList(string className) { }
        public void RemoveFromClassList(string className) { }
        public void SetEnabled(bool enabled) { }
        public VisualElement? Q(string? name = null, string? className = null) => null;
        public T? Q<T>(string? name = null, string? className = null) where T : VisualElement => default;
    }

    internal class Label : VisualElement
    {
        public string text { get; set; } = string.Empty;
        public Label() { }
        public Label(string text) { this.text = text; }
    }

    internal class Button : VisualElement
    {
        public string text { get; set; } = string.Empty;
        public event Action? clicked;
        public Button() { }
        public Button(Action? clickEvent) { clicked += clickEvent; }
        public void InvokeClicked() => clicked?.Invoke();
    }

    internal class TextField : VisualElement
    {
        public string label { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }

    internal class Toggle : VisualElement
    {
        public string label { get; set; } = string.Empty;
        public bool value { get; set; }
        public Toggle() { }
        public Toggle(string label) { this.label = label; }
    }

    internal class Slider : VisualElement
    {
        public string label { get; set; } = string.Empty;
        public float value { get; set; }
        public float lowValue { get; set; }
        public float highValue { get; set; } = 1f;
        public Slider() { }
        public Slider(string label, float low, float high) { this.label = label; lowValue = low; highValue = high; }
    }

    internal class ProgressBar : VisualElement
    {
        public string title { get; set; } = string.Empty;
        public float value { get; set; }
        public float lowValue { get; set; }
        public float highValue { get; set; } = 100f;
    }

    internal class ScrollView : VisualElement { }
    internal class Foldout : VisualElement
    {
        public string text { get; set; } = string.Empty;
        public bool value { get; set; } = true;
    }

    internal class ListView : VisualElement
    {
        public IList? itemsSource { get; set; }
        public Func<VisualElement>? makeItem { get; set; }
        public Action<VisualElement, int>? bindItem { get; set; }
        public int fixedItemHeight { get; set; } = 25;
    }

    internal class TemplateContainer : VisualElement { }

    internal class UIDocument : Behaviour
    {
        public VisualElement rootVisualElement { get; } = new();
        public PanelSettings? panelSettings { get; set; }
    }

    internal class PanelSettings : ScriptableObject
    {
        public float referenceDpi { get; set; } = 96f;
        public PanelScaleMode scaleMode { get; set; }
        public Vector2Int referenceResolution { get; set; } = new(1920, 1080);
        public PanelScreenMatchMode screenMatchMode { get; set; }
        public float match { get; set; }
        public ThemeStyleSheet? themeStyleSheet { get; set; }
    }

    internal enum PanelScaleMode { ConstantPixelSize, ConstantPhysicalSize, ScaleWithScreenSize }
    internal enum PanelScreenMatchMode { MatchWidthOrHeight, Shrink, Expand }

    internal class ThemeStyleSheet : ScriptableObject { }

    internal class StyleSheet : ScriptableObject { }

    internal enum DisplayStyle { Flex, None }

    internal struct StyleFont
    {
        public Font value;
        public StyleFont(Font font) { value = font; }
        public static implicit operator StyleFont(Font f) => new(f);
    }

    internal interface IStyle
    {
        StyleLength width { get; set; }
        StyleLength height { get; set; }
        StyleLength maxWidth { get; set; }
        StyleLength maxHeight { get; set; }
        StyleLength marginTop { get; set; }
        StyleLength marginBottom { get; set; }
        StyleLength marginLeft { get; set; }
        StyleLength marginRight { get; set; }
        StyleLength paddingTop { get; set; }
        StyleLength paddingBottom { get; set; }
        StyleLength paddingLeft { get; set; }
        StyleLength paddingRight { get; set; }
        StyleColor backgroundColor { get; set; }
        StyleColor color { get; set; }
        StyleLength fontSize { get; set; }
        StyleEnum<FlexDirection> flexDirection { get; set; }
        StyleFloat flexGrow { get; set; }
        StyleFloat flexShrink { get; set; }
        StyleEnum<Align> alignItems { get; set; }
        StyleEnum<Align> alignSelf { get; set; }
        StyleEnum<Justify> justifyContent { get; set; }
        StyleEnum<Position> position { get; set; }
        StyleLength top { get; set; }
        StyleLength right { get; set; }
        StyleLength bottom { get; set; }
        StyleLength left { get; set; }
        StyleLength borderTopWidth { get; set; }
        StyleLength borderBottomWidth { get; set; }
        StyleLength borderLeftWidth { get; set; }
        StyleLength borderRightWidth { get; set; }
        StyleColor borderTopColor { get; set; }
        StyleColor borderBottomColor { get; set; }
        StyleColor borderLeftColor { get; set; }
        StyleColor borderRightColor { get; set; }
        StyleLength borderTopLeftRadius { get; set; }
        StyleLength borderTopRightRadius { get; set; }
        StyleLength borderBottomLeftRadius { get; set; }
        StyleLength borderBottomRightRadius { get; set; }
        StyleEnum<DisplayStyle> display { get; set; }
        StyleEnum<Overflow> overflow { get; set; }
        StyleEnum<WhiteSpace> whiteSpace { get; set; }
        StyleEnum<TextAnchor> unityTextAlign { get; set; }
        StyleEnum<FontStyle> unityFontStyleAndWeight { get; set; }
        StyleFont unityFont { get; set; }
    }

    internal struct StyleLength
    {
        public float value;
        public static implicit operator StyleLength(float v) => new() { value = v };
        public static implicit operator StyleLength(int v) => new() { value = v };
        public static implicit operator StyleLength(Length v) => new() { value = v.value };
    }

    internal struct StyleColor
    {
        public Color value;
        public static implicit operator StyleColor(Color c) => new() { value = c };
    }

    internal struct StyleFloat
    {
        public float value;
        public static implicit operator StyleFloat(float v) => new() { value = v };
    }

    internal struct StyleEnum<T> where T : struct
    {
        public T value;
        public static implicit operator StyleEnum<T>(T v) => new() { value = v };
    }

    internal struct Length
    {
        public float value;
        public LengthUnit unit;
        public Length(float value) { this.value = value; unit = LengthUnit.Pixel; }
        public Length(float value, LengthUnit unit) { this.value = value; this.unit = unit; }
        public static Length Percent(float v) => new(v, LengthUnit.Percent);
        public static Length Auto() => new(0);
    }

    internal enum LengthUnit { Pixel, Percent }
    internal enum FlexDirection { Column, ColumnReverse, Row, RowReverse }
    internal enum Align { Auto, FlexStart, Center, FlexEnd, Stretch }
    internal enum Justify { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround }
    internal enum Position { Relative, Absolute }
    internal enum Overflow { Visible, Hidden }
    internal enum WhiteSpace { Normal, NoWrap }

    internal sealed class StyleStub : IStyle
    {
        public StyleLength width { get; set; }
        public StyleLength height { get; set; }
        public StyleLength maxWidth { get; set; }
        public StyleLength maxHeight { get; set; }
        public StyleLength marginTop { get; set; }
        public StyleLength marginBottom { get; set; }
        public StyleLength marginLeft { get; set; }
        public StyleLength marginRight { get; set; }
        public StyleLength paddingTop { get; set; }
        public StyleLength paddingBottom { get; set; }
        public StyleLength paddingLeft { get; set; }
        public StyleLength paddingRight { get; set; }
        public StyleColor backgroundColor { get; set; }
        public StyleColor color { get; set; }
        public StyleLength fontSize { get; set; }
        public StyleEnum<FlexDirection> flexDirection { get; set; }
        public StyleFloat flexGrow { get; set; }
        public StyleFloat flexShrink { get; set; }
        public StyleEnum<Align> alignItems { get; set; }
        public StyleEnum<Align> alignSelf { get; set; }
        public StyleEnum<Justify> justifyContent { get; set; }
        public StyleEnum<Position> position { get; set; }
        public StyleLength top { get; set; }
        public StyleLength right { get; set; }
        public StyleLength bottom { get; set; }
        public StyleLength left { get; set; }
        public StyleLength borderTopWidth { get; set; }
        public StyleLength borderBottomWidth { get; set; }
        public StyleLength borderLeftWidth { get; set; }
        public StyleLength borderRightWidth { get; set; }
        public StyleColor borderTopColor { get; set; }
        public StyleColor borderBottomColor { get; set; }
        public StyleColor borderLeftColor { get; set; }
        public StyleColor borderRightColor { get; set; }
        public StyleLength borderTopLeftRadius { get; set; }
        public StyleLength borderTopRightRadius { get; set; }
        public StyleLength borderBottomLeftRadius { get; set; }
        public StyleLength borderBottomRightRadius { get; set; }
        public StyleEnum<DisplayStyle> display { get; set; }
        public StyleEnum<Overflow> overflow { get; set; }
        public StyleEnum<WhiteSpace> whiteSpace { get; set; }
        public StyleEnum<TextAnchor> unityTextAlign { get; set; }
        public StyleEnum<FontStyle> unityFontStyleAndWeight { get; set; }
        public StyleFont unityFont { get; set; }
    }
}
#endif
