using System;

namespace SengokuWarSim.Sim
{
    /// <summary>
    /// 手続き生成した戦場の地形データ。1 セル = 1m。
    /// 丘 (ガウス山)、中央帯の水田、両端の林、南北に走る街道を持つ。
    /// メッシュ生成 (Unity 側) と判定 (Sim 側) が同じデータを参照する。
    /// </summary>
    public sealed class TerrainMap : ITerrainQuery
    {
        public struct Hill
        {
            public Vec2 Center;
            public float Radius;
            public float Height;
        }

        public int Size { get; }
        public float HalfSize => Size * 0.5f;
        public int VertexCount => Size + 1;
        public Hill[] Hills { get; private set; }

        readonly TerrainKind[] _kinds;
        readonly float[] _heights;
        readonly int _seed;

        public const float PaddyDepth = 0.3f;
        public const float HillThreshold = 1.6f;

        TerrainMap(int size, int seed)
        {
            if (size < 8 || (size & 1) != 0) throw new ArgumentException("size must be even and >= 8", nameof(size));
            Size = size;
            _seed = seed;
            _kinds = new TerrainKind[size * size];
            _heights = new float[(size + 1) * (size + 1)];
        }

        public static TerrainMap Generate(int size, int seed)
        {
            var map = new TerrainMap(size, seed);
            map.Build();
            return map;
        }

        void Build()
        {
            var rng = new Random(_seed);
            float half = HalfSize;

            // 丘: 中央帯に 3 つ。戦術的に意味のある位置に置く。
            Hills = new Hill[3];
            for (int i = 0; i < Hills.Length; i++)
            {
                Hills[i] = new Hill
                {
                    Center = new Vec2(
                        (float)(rng.NextDouble() * 2 - 1) * half * 0.6f,
                        (float)(rng.NextDouble() * 2 - 1) * half * 0.35f),
                    Radius = 8f + (float)rng.NextDouble() * 6f,
                    Height = 2.5f + (float)rng.NextDouble() * 2f,
                };
            }

            for (int vz = 0; vz <= Size; vz++)
            for (int vx = 0; vx <= Size; vx++)
                _heights[vz * VertexCount + vx] = HillHeight(vx - half, vz - half);

            for (int cz = 0; cz < Size; cz++)
            for (int cx = 0; cx < Size; cx++)
                _kinds[cz * Size + cx] = ClassifyCell(cx, cz);

            // 水田は周囲より少し低い
            for (int vz = 0; vz <= Size; vz++)
            for (int vx = 0; vx <= Size; vx++)
            {
                int paddyNeighbours = 0;
                for (int dz = -1; dz <= 0; dz++)
                for (int dx = -1; dx <= 0; dx++)
                    if (KindAtCell(vx + dx, vz + dz) == TerrainKind.Paddy) paddyNeighbours++;
                if (paddyNeighbours == 4) _heights[vz * VertexCount + vx] -= PaddyDepth;
                else if (paddyNeighbours > 0) _heights[vz * VertexCount + vx] -= PaddyDepth * 0.5f;
            }
        }

        float HillHeight(float x, float z)
        {
            float h = 0f;
            foreach (var hill in Hills)
            {
                float dx = x - hill.Center.X;
                float dz = z - hill.Center.Z;
                float r2 = hill.Radius * hill.Radius;
                h += hill.Height * (float)Math.Exp(-(dx * dx + dz * dz) / (r2 * 0.5f));
            }
            return h;
        }

        TerrainKind ClassifyCell(int cx, int cz)
        {
            float half = HalfSize;
            float x = cx - half + 0.5f;
            float z = cz - half + 0.5f;

            if (Math.Abs(x) < 1.5f) return TerrainKind.Road;
            if (HillHeight(x, z) > HillThreshold) return TerrainKind.Hill;

            if (Math.Abs(x) > half * 0.72f && ValueNoise(cx, cz) > 0.35f) return TerrainKind.Forest;

            bool inPaddyBand = Math.Abs(z) < half * 0.28f && Math.Abs(x) < half * 0.6f;
            if (inPaddyBand)
            {
                // 8m × 6m の田が畦 (Field) で区切られている
                int px = (int)Math.Floor((x + 1000f) / 8f);
                int pz = (int)Math.Floor((z + 1000f) / 6f);
                float fx = (x + 1000f) - px * 8f;
                float fz = (z + 1000f) - pz * 6f;
                bool dike = fx < 1f || fz < 1f;
                bool planted = (px + pz) % 3 != 0;
                if (!dike && planted) return TerrainKind.Paddy;
            }
            return TerrainKind.Field;
        }

        /// <summary>0..1 の決定論的ノイズ。</summary>
        float ValueNoise(int x, int z)
        {
            unchecked
            {
                int h = _seed * 374761393 + x * 668265263 + z * 2147483647;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        // ------------------------------------------------------------ queries

        public TerrainKind KindAtCell(int cx, int cz)
        {
            cx = Math.Max(0, Math.Min(Size - 1, cx));
            cz = Math.Max(0, Math.Min(Size - 1, cz));
            return _kinds[cz * Size + cx];
        }

        public TerrainKind KindAt(Vec2 p)
        {
            return KindAtCell((int)Math.Floor(p.X + HalfSize), (int)Math.Floor(p.Z + HalfSize));
        }

        public float VertexHeight(int vx, int vz)
        {
            vx = Math.Max(0, Math.Min(Size, vx));
            vz = Math.Max(0, Math.Min(Size, vz));
            return _heights[vz * VertexCount + vx];
        }

        public float HeightAt(Vec2 p)
        {
            float gx = p.X + HalfSize;
            float gz = p.Z + HalfSize;
            int x0 = (int)Math.Floor(gx);
            int z0 = (int)Math.Floor(gz);
            float tx = gx - x0;
            float tz = gz - z0;
            float h00 = VertexHeight(x0, z0);
            float h10 = VertexHeight(x0 + 1, z0);
            float h01 = VertexHeight(x0, z0 + 1);
            float h11 = VertexHeight(x0 + 1, z0 + 1);
            float a = h00 + (h10 - h00) * tx;
            float b = h01 + (h11 - h01) * tx;
            return a + (b - a) * tz;
        }

        /// <summary>各地形の面積 (セル数)。デバッグ・検証用。</summary>
        public int CountCells(TerrainKind kind)
        {
            int n = 0;
            foreach (var k in _kinds) if (k == kind) n++;
            return n;
        }
    }
}
