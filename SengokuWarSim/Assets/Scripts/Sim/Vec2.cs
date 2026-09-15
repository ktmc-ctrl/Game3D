using System;

namespace SengokuWarSim.Sim
{
    /// <summary>
    /// 地面平面上の2Dベクトル。X が東西、Z が南北 (Unity の XZ 平面と同じ向き)。
    /// UnityEngine に依存しないため Sim アセンブリ内で完結する。
    /// </summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Z;

        public Vec2(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);
        public static readonly Vec2 North = new Vec2(0f, 1f);
        public static readonly Vec2 South = new Vec2(0f, -1f);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator *(float s, Vec2 a) => a * s;

        public float SqrLength => X * X + Z * Z;
        public float Length => (float)Math.Sqrt(SqrLength);

        public Vec2 Normalized
        {
            get
            {
                float len = Length;
                return len > 1e-6f ? this * (1f / len) : Zero;
            }
        }

        /// <summary>左90度回転したベクトル。</summary>
        public Vec2 Perpendicular => new Vec2(-Z, X);

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float SqrDistance(Vec2 a, Vec2 b) => (a - b).SqrLength;
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

        /// <summary>ラジアン角から向きを作る。0 = +Z (北)、時計回りに増加 (Unity の Y 回転と同じ)。</summary>
        public static Vec2 FromAngle(float radians) => new Vec2((float)Math.Sin(radians), (float)Math.Cos(radians));

        /// <summary>この向きの Y 回転角 (ラジアン)。</summary>
        public float ToAngle() => (float)Math.Atan2(X, Z);

        public Vec2 Clamped(float halfExtent)
        {
            return new Vec2(
                Math.Max(-halfExtent, Math.Min(halfExtent, X)),
                Math.Max(-halfExtent, Math.Min(halfExtent, Z)));
        }

        public bool Equals(Vec2 other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Z.GetHashCode();
        public override string ToString() => $"({X:0.00}, {Z:0.00})";
    }
}
