using ForgeFlow.Core.Entities;
using UnityEngine;

namespace ForgeFlow.Presentation.Unity.Extensions
{
    /// <summary>Extension methods to convert ForgeFlow.Core RPG structs into Unity types.
    /// Lives exclusively in the Presentation layer — Core has zero Unity knowledge.
    /// </summary>
    internal static class CoreToUnityExtensions
    {
        // --- ColorRPG ↔ UnityEngine.Color ---

        /// <summary>Converts a Core ColorRPG to a Unity Color.</summary>
        public static Color ToUnityColor(this ColorRPG color) =>
            new Color(color.R, color.G, color.B, color.A);

        /// <summary>Converts a Unity Color to a Core ColorRPG.</summary>
        public static ColorRPG ToColorRPG(this Color color) =>
            new ColorRPG(color.r, color.g, color.b, color.a);

        // --- Vector2RPG ↔ UnityEngine.Vector2 ---

        /// <summary>Converts a Core Vector2RPG to a Unity Vector2.</summary>
        public static Vector2 ToUnityVector2(this Vector2RPG v) =>
            new Vector2(v.X, v.Y);

        /// <summary>Converts a Unity Vector2 to a Core Vector2RPG.</summary>
        public static Vector2RPG ToVector2RPG(this Vector2 v) =>
            new Vector2RPG(v.x, v.y);

        // --- Vector3RPG ↔ UnityEngine.Vector3 ---

        /// <summary>Converts a Core Vector3RPG to a Unity Vector3.</summary>
        public static Vector3 ToUnityVector3(this Vector3RPG v) =>
            new Vector3(v.X, v.Y, v.Z);

        /// <summary>Converts a Unity Vector3 to a Core Vector3RPG.</summary>
        public static Vector3RPG ToVector3RPG(this Vector3 v) =>
            new Vector3RPG(v.x, v.y, v.z);

        // --- QuaternionRPG ↔ UnityEngine.Quaternion ---

        /// <summary>Converts a Core QuaternionRPG to a Unity Quaternion.</summary>
        public static Quaternion ToUnityQuaternion(this QuaternionRPG q) =>
            new Quaternion { x = q.X, y = q.Y, z = q.Z, w = q.W };

        /// <summary>Converts a Unity Quaternion to a Core QuaternionRPG.</summary>
        public static QuaternionRPG ToQuaternionRPG(this Quaternion q) =>
            new QuaternionRPG(q.x, q.y, q.z, q.w);

        // --- RectRPG ↔ UnityEngine.Rect ---

        /// <summary>Converts a Core RectRPG to a Unity Rect.</summary>
        public static Rect ToUnityRect(this RectRPG r) =>
            new Rect(r.X, r.Y, r.Width, r.Height);

        /// <summary>Converts a Unity Rect to a Core RectRPG.</summary>
        public static RectRPG ToRectRPG(this Rect r) =>
            new RectRPG(r.x, r.y, r.width, r.height);

        // --- GridPosRPG → Unity Vector3 ---

        /// <summary>Converts a GridPosRPG to a Unity Vector3 (X → x, Y → z, y = 0).</summary>
        public static Vector3 ToUnityVector3(this GridPosRPG pos) =>
            new Vector3(pos.X, 0f, pos.Y);

        /// <summary>Converts a GridPosRPG to a Unity Vector3 with a custom y height.</summary>
        public static Vector3 ToUnityVector3(this GridPosRPG pos, float height) =>
            new Vector3(pos.X, height, pos.Y);

        // --- GridRelRPG → Unity Vector3 ---

        /// <summary>Converts a GridRelRPG to a Unity Vector3 offset.</summary>
        public static Vector3 ToUnityVector3(this GridRelRPG rel) =>
            new Vector3(rel.DX, 0f, rel.DY);

        /// <summary>Converts a GridPosRPG to a Unity Vector2 using grid-space coordinates.</summary>
        public static Vector2 ToUnityVector2(this GridPosRPG pos) =>
            new Vector2(pos.X, pos.Y);

        /// <summary>Converts a GridRelRPG to a Unity Vector2 using relative grid-space coordinates.</summary>
        public static Vector2 ToUnityVector2(this GridRelRPG rel) =>
            new Vector2(rel.DX, rel.DY);

        // --- TileSpec → Unity Color ---

        /// <summary>Gets the Unity color tint for a tile.</summary>
        public static Color ToUnityTileColor(this TileSpec tile) =>
            tile.TileColor.ToUnityColor();

        // --- Unity Vector3 → Grid ---

        /// <summary>Snaps a Unity world position to the nearest GridPosRPG.</summary>
        public static GridPosRPG ToGridPosRPG(this Vector3 worldPos) =>
            new GridPosRPG(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));

        // --- GameId → display ---

        /// <summary>Returns a Unity-friendly short display string for a GameId.</summary>
        public static string ToDisplayString(this GameId id) => id.ToShortHex();
    }
}
