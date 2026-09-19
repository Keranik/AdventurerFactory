namespace ForgeFlow.Core.Entities;

/// <summary>
/// Immutable RPG-style quaternion for rotations. Pure .NET — zero Unity references.
/// Use <c>ToUnityQuaternion()</c> in the Presentation layer to convert at the last moment.
/// </summary>
[Serializable]
public readonly struct QuaternionRPG : IEquatable<QuaternionRPG>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }

    public QuaternionRPG(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    // --- Named Factories ---
    public static QuaternionRPG Identity => new(0f, 0f, 0f, 1f);

    /// <summary>
    /// Creates a quaternion from Euler angles (degrees).
    /// </summary>
    public static QuaternionRPG Euler(float pitchDeg, float yawDeg, float rollDeg)
    {
        const float deg2Rad = (float)(Math.PI / 180.0);
        float halfPitch = pitchDeg * deg2Rad * 0.5f;
        float halfYaw = yawDeg * deg2Rad * 0.5f;
        float halfRoll = rollDeg * deg2Rad * 0.5f;

        float sp = (float)Math.Sin(halfPitch);
        float cp = (float)Math.Cos(halfPitch);
        float sy = (float)Math.Sin(halfYaw);
        float cy = (float)Math.Cos(halfYaw);
        float sr = (float)Math.Sin(halfRoll);
        float cr = (float)Math.Cos(halfRoll);

        return new(
            cy * sp * cr + sy * cp * sr,
            sy * cp * cr - cy * sp * sr,
            cy * cp * sr - sy * sp * cr,
            cy * cp * cr + sy * sp * sr
        );
    }

    /// <summary>Creates a rotation that looks in the given forward direction (Y-up).</summary>
    public static QuaternionRPG LookRotation(Vector3RPG forward)
    {
        if (forward.SqrMagnitude < 1e-6f) return Identity;
        // Simplified: only compute yaw + pitch from forward direction
        float yaw = (float)Math.Atan2(forward.X, forward.Z) * (180f / (float)Math.PI);
        float pitch = (float)Math.Asin(-forward.Y / forward.Magnitude) * (180f / (float)Math.PI);
        return Euler(pitch, yaw, 0f);
    }

    // --- Math ---

    public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

    public QuaternionRPG Normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > 1e-6f ? new(X / mag, Y / mag, Z / mag, W / mag) : Identity;
        }
    }

    public QuaternionRPG Inverse => new(-X, -Y, -Z, W);

    public static QuaternionRPG operator *(QuaternionRPG a, QuaternionRPG b) =>
        new(a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    /// <summary>Rotates a point by this quaternion.</summary>
    public Vector3RPG Rotate(Vector3RPG point)
    {
        var q = new QuaternionRPG(point.X, point.Y, point.Z, 0f);
        var result = this * q * Inverse;
        return new Vector3RPG(result.X, result.Y, result.Z);
    }

    public static QuaternionRPG Slerp(QuaternionRPG a, QuaternionRPG b, float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        if (dot < 0f)
        {
            b = new QuaternionRPG(-b.X, -b.Y, -b.Z, -b.W);
            dot = -dot;
        }
        if (dot > 0.9995f)
        {
            return new QuaternionRPG(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t).Normalized;
        }
        float theta = (float)Math.Acos(dot);
        float sinTheta = (float)Math.Sin(theta);
        float wa = (float)Math.Sin((1f - t) * theta) / sinTheta;
        float wb = (float)Math.Sin(t * theta) / sinTheta;
        return new QuaternionRPG(
            wa * a.X + wb * b.X,
            wa * a.Y + wb * b.Y,
            wa * a.Z + wb * b.Z,
            wa * a.W + wb * b.W);
    }

    // --- Equality ---

    public bool Equals(QuaternionRPG other) =>
        Math.Abs(X - other.X) < 1e-6f &&
        Math.Abs(Y - other.Y) < 1e-6f &&
        Math.Abs(Z - other.Z) < 1e-6f &&
        Math.Abs(W - other.W) < 1e-6f;

    public static bool operator ==(QuaternionRPG a, QuaternionRPG b) => a.Equals(b);
    public static bool operator !=(QuaternionRPG a, QuaternionRPG b) => !a.Equals(b);

    public override bool Equals(object? obj) => obj is QuaternionRPG other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3}, {W:F3})";
}
